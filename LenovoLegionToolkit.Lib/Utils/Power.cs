﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Threading.Tasks;

namespace LenovoLegionToolkit.Lib.Utils
{
    public class PowerPlan
    {
        public string InstanceID { get; }
        public string Name { get; }
        public bool IsActive { get; }
        public string Guid => InstanceID.Split("\\").Last().Replace("{", "").Replace("}", "");

        public PowerPlan(string instanceID, string name, bool isActive)
        {
            InstanceID = instanceID;
            Name = name;
            IsActive = isActive;
        }

        public override string ToString() => Name;
    }

    public static class Power
    {
        public static async Task RestartAsync()
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Restarting...");

            await CMD.RunAsync("shutdown", "/r /t 0").ConfigureAwait(false);
        }

        public static async Task<PowerPlan[]> GetPowerPlansAsync()
        {
            var result = await WMI.ReadAsync("root\\CIMV2\\power",
                            $"SELECT * FROM Win32_PowerPlan",
                            Create).ConfigureAwait(false);
            return result.ToArray();
        }

        public static async Task ActivatePowerPlanAsync(PowerModeState powerModeState, bool alwaysActivateDefaults = false)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Activating... [powerModeState={powerModeState}, alwaysActivateDefaults={alwaysActivateDefaults}]");

            var powerPlanId = Settings.Instance.PowerPlans.GetValueOrDefault(powerModeState);
            var isDefault = false;

            if (powerPlanId == null)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Power plan for power mode {powerModeState} was not found in settings");

                powerPlanId = GetDefaultPowerPlanId(powerModeState);
                isDefault = true;
            }

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Power plan to be activated is {powerPlanId} [isDefault={isDefault}]");

            if (!await ShouldActivateAsync(alwaysActivateDefaults, isDefault).ConfigureAwait(false))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Power plan {powerPlanId} will not be activated [isDefault={isDefault}]");

                return;
            }

            var powerPlan = (await GetPowerPlansAsync()).FirstOrDefault(pp => pp.InstanceID.Contains(powerPlanId));
            if (powerPlan == null)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Power plan {powerPlanId} was not found");

                return;
            }
            if (powerPlan.IsActive)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Power plan {powerPlanId} is already active");

                return;
            }

            await CMD.RunAsync("powercfg", $"/s {powerPlan.Guid}").ConfigureAwait(false);

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Power plan {powerPlan.Guid} activated");
        }

        private static async Task<bool> ShouldActivateAsync(bool alwaysActivateDefaults, bool isDefault)
        {
            var overide = Settings.Instance.ActivatePowerProfilesWithVantageEnabled;
            if (overide)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Activate power profiles with Vantage is enabled");

                return true;
            }

            if (isDefault && alwaysActivateDefaults)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Power plan is default and always active defaults is set");

                return true;
            }

            var status = await Vantage.GetStatusAsync().ConfigureAwait(false);
            if (status == VantageStatus.NotFound || status == VantageStatus.Disabled)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Vantage is active [status={status}]");

                return true;
            }

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Criteria for activation not met [overide={overide}, isDefault={isDefault}, alwaysActivateDefaults={alwaysActivateDefaults}, status={status}]");

            return false;
        }

        private static PowerPlan Create(PropertyDataCollection properties)
        {
            var instanceId = (string)properties["InstanceID"].Value;
            var name = (string)properties["ElementName"].Value;
            var isActive = (bool)properties["IsActive"].Value;
            return new(instanceId, name, isActive);
        }

        private static string GetDefaultPowerPlanId(PowerModeState state) => state switch
        {
            PowerModeState.Quiet => "16edbccd-dee9-4ec4-ace5-2f0b5f2a8975",
            PowerModeState.Balance => "85d583c5-cf2e-4197-80fd-3789a227a72c",
            PowerModeState.Performance => "52521609-efc9-4268-b9ba-67dea73f18b2",
            _ => throw new InvalidOperationException("Unknown state"),
        };
    }
}
