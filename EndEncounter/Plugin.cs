using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using EndEncounter.Windows;
using Dalamud.Game.ClientState.Conditions;
using System.Diagnostics;
using System;
using System.Threading.Tasks;

namespace EndEncounter;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;

    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;

    [PluginService] internal static IClientState ClientState { get; private set; } = null!;

    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    [PluginService] internal static ICondition Condition { get; private set; } = null!;

    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;

    private const string CommandName = "/ee";

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("EndEncounter");

    private ConfigWindow ConfigWindow { get; init; }

    internal Stopwatch CombatTimer = new Stopwatch();


    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        ConfigWindow = new ConfigWindow(this);

        WindowSystem.AddWindow(ConfigWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open the configuration window."
        });

        PluginInterface.UiBuilder.Draw += DrawUI;

        PluginInterface.UiBuilder.OpenMainUi += ToggleConfigUI;

        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;

        Log.Information("Initialized");

        Condition.ConditionChange += OnConditionChange;
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);

        Condition.ConditionChange -= OnConditionChange;

        CombatTimer.Stop();
    }

    private void OnCommand(string command, string args)
    {
        ToggleConfigUI();
    }

    private void DrawUI() => WindowSystem.Draw();

    public void ToggleConfigUI() => ConfigWindow.Toggle();

    private void OnConditionChange(ConditionFlag flag, bool value)
    {
        if (flag == ConditionFlag.LoggingOut) return;

        if (!Condition.Any()) return;

        if (ClientState?.LocalPlayer?.ClassJob == null) return;

        if (flag == ConditionFlag.InCombat && CombatTimer.IsRunning)
        {
            Log.Debug("Exiting Combat");

            new Task(() =>
            {
                try
                {
                    var secondsToWait = Configuration.Seconds;

                    ACTHelper.EndACTEncounter(ChatGui, secondsToWait * 1000);

                    Log.Information($"Sent ACT end message after {secondsToWait} seconds");
                }
                catch (Exception err)
                {
                    Log.Warning("Unable to send ACT end message: {0}", err);
                }
            }).Start();

            try
            {
                CombatTimer.Stop();
                CombatTimer.Reset();
            }
            catch (Exception err)
            {
                Log.Warning("Unable to stop and reset CombatTimer: {0}", err);
            }

        }
        else if (flag == ConditionFlag.InCombat && !CombatTimer.IsRunning)
        {
            Log.Debug("Entering Combat");

            try
            {
                CombatTimer.Start();
            }
            catch (Exception err)
            {
                Log.Warning("Unable to start CombatTimer: {0}", err);
            }
        }
    }
}
