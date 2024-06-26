﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace EGMSettings
{
    /// <summary>
    /// Interaction logic for EGMSettings.xaml
    /// </summary>
    public partial class SettingsPanel : NotifyPropertyChangedWindowBase
    {
        #region SystemVars
        public const string currentBuild = "v3.0.5";
        public MEGame mode = MEGame.ME3;
        public string egmPath = null;
        public string[] egmMetaData;
        public Version egmVersion = new Version("0.0.0");
        public int defUpdateCount = 0;
        public bool squadmate;
        public bool framework;
        public bool normandy;
        public bool galMap;
        public bool fixCutscenes;
        public bool mirandaMod;
        public bool armorMod;
        public bool shipwreckMod;

        private string _header_TITLE = $"Expanded Galaxy Mod Settings {currentBuild}";
        public string header_TITLE { get => _header_TITLE; set => SetProperty(ref _header_TITLE, value); }
        private int _currentView;
        public int currentView { get => _currentView; set => SetProperty(ref _currentView, value); }
        private string displayedHelp;
        private string binPath;
        private string verPath;
        private string gamePath;
        private string me3Path;
        private string le3Path;
        private List<ModSetting> Settings = new List<ModSetting>();
        private const string plotcmd = "InitPlotManagerValueByIndex ";
        private const string boolcmd = " bool ";
        private const string intcmd = " int ";
        private string _statusText = currentBuild;
        public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }
        private string _diagnostic;
        public string Diagnostic { get => _diagnostic; set => SetProperty(ref _diagnostic, value); }
        private string _diagnosticb;
        public string DiagnosticB { get => _diagnosticb; set => SetProperty(ref _diagnosticb, value); }
        private bool needsSave;
        public ICommand SaveCommand { get; set; }
        public ICommand LoadCommand { get; set; }
        public ICommand NextCommand { get; set; }
        public ICommand BackCommand { get; set; }
        public ICommand FinishCommand { get; set; }
        public ICommand ResetDefaultCommand { get; set; }
        public ICommand ShowDiagnosticsCommand { get; set; }
        public ICommand AutoTOCCommand { get; set; }
        public ICommand SwitchGameCommand { get; set; }
        public ICommand ExportCommand { get; set; }
        public ICommand ImportCommand { get; set; }

        private bool CanNextCommand()
        {
            return currentView < 6;
        }
        private bool CanBackCommand()
        {
            return currentView > 0;
        }

        public enum MEGame
        {
            ME3 = 0,
            LE3 = 1
        }

        public const string welcome_TITLE = "Welcome to Expanded Galaxy Mod settings";
        public const string welcome_TXT = "You can adjust these settings to select how different parts of EGM function. The settings will be checked and updated every time you reload the Normandy. Most settings can be changed dynamically during a playthough, where they cannot it is clearly marked. However, with mission timings and war asset settings we recommend making your initial choices before starting your playthrough.";

        private int _updateVersion = 0; //This gets incremented every save. Only will get picked up if changed.
        public int UpdateVersion { get => _updateVersion; set { SetProperty(ref _updateVersion, value); } }
        private const int UpdateVersionInt = 29402;
        #endregion

        #region ModVars
        public const string Mod_Help_TITLE = "Various Mod Settings";
        public const string Mod_Help_TXT = "Hover the mouse over the settings to see more information.";
        private int _modWARBeta_choice = 0; //NEEDS +1 ADDED
        public int ModWARBeta_choice { get => _modWARBeta_choice; set { SetProperty(ref _modWARBeta_choice, value); needsSave = true; } }
        private ObservableCollection<string> _modWARBeta_cln = new ObservableCollection<string>() { "Easy: Default ME3 setting (6200 / 6200)", "Galactic war: (6800 / 6250)", "Extinction event: (7100 / 6500)" };
        public ObservableCollection<string> ModWARBeta_cln { get => _modWARBeta_cln; }
        private const string ModWARBeta_TITLE = "War Asset Beta - Difficulty Settings";
        private const string ModWARBeta_TXT = "THIS IS ONLY APPLIED IF YOU HAVE THE BETA INSTALLED.\n\nGalactic Readiness is permanently set to 100% in the Beta (no need for multiplayer). Target score depends on difficulty and whether you are playing with an ME2 import. \n\nIMPORTANT: once Priority: Surkesh is finished this setting cannot be changed. The first time you load the Normandy up after this mission, it will be fixed.\n\n";
        private const string ModWARBeta2_TXT = "Difficulty if your playthrough includes ME2:\nEasy Default ME3 setting (Best outcome requires EMS 6200)\nGalactic war (target: 6800, higher chance of worse outcomes)\nExtinction event (target: 7100, tough choices required)\n\n";
        private const string ModWARBeta3_TXT = "Difficulty if your playthrough does not include ME2:\nEasy Default ME3 setting (Best outcome requires EMS 6200)\nGalactic war (target: 6250, higher chance of worse outcomes)\nExtinction event (target: 6500, tough choices required)\n\n";

        private int _modQP_choice = 0;
        public int ModQP_choice { get => _modQP_choice;
            set {
                SetProperty(ref _modQP_choice, value);
                needsSave = true;
                if (ModQP_choice == 1)
                {
                    ModAssign_choice = 1;
                    ModWARBeta_choice = 0;
                    modBeta_cbx.IsEnabled = false;
                    modAssgn_cbx.IsEnabled = false;
                }
                else
                {
                    modBeta_cbx.IsEnabled = true;
                    modAssgn_cbx.IsEnabled = true;
                }

            } }
        private ObservableCollection<string> _modQP_cln = new ObservableCollection<string>() { "Quick Play Mode off", "Quick Play Mode on" };
        public ObservableCollection<string> ModQP_cln { get => _modQP_cln; }
        private const string ModQP_TITLE = "Quick Play Mode";
        private const string ModQP_TXT = "This mode is designed for players who want a quick playthrough focused on combat and story, and without RPG elements such as the War Asset System.\n\nThis mode:\n- Disables the war asset system so no need to recover war assets or complete side missions.\n- Gives the player the highest war asset score\n- Hides every star cluster in the galaxy map which doesn't have a playable combat mission.\n- Disables the War Asset Terminals.\n\nNOTE THIS MUST BE SET BEFORE THE END OF THE PROLOGUE (CITADEL I)\nONCE SET IT CANNOT BE UNSET (EVEN IF EGM IS UNINSTALLED YOUR SAVE WILL AUTOMATICALLY BE SET TO MAXIMUM).";

        private int _modAssign_choice = 0;
        public int ModAssign_choice { get => _modAssign_choice; set { SetProperty(ref _modAssign_choice, value); needsSave = true; } }
        private ObservableCollection<string> _modAssign_cln = new ObservableCollection<string>() { "EGM Assignments on (default)", "EGM Assignments off" };
        public ObservableCollection<string> ModAssign_cln { get => _modAssign_cln; }
        private const string ModAssign_TITLE = "EGM Assignments";
        private const string ModAssign_TXT = "EGM has added 15 assignments.  These are short (often text based) fetch quests, similar to the ones in the default game but with an added twist. They give extra war assets, credits, choices and paragon/renegade bonuses.\n\nThe assignments include the Evacuation of Thessia minigame and the quest that leads to the Prothean Cybernetics.\n\nIf you don't want the added assignments switch this off.\n\nNote once the assignment has been given, it will remain active and can be completed.  If you switch off this setting after you have completed the assignment you will keep the rewards.";
        private const string ModAssignLE_TXT = "EGM Galactic War module has added 11 new assignments and Miranda Mod has 2 more.  These are short (often text based) fetch quests, similar to the ones in the default game but with an added twist. They give extra war assets, credits, choices and paragon/renegade bonuses.\n\nNote once the assignment has been given, it will remain active and can be completed.  If you switch off this setting after you have completed the assignment you will keep the rewards.";

        private int _modEggs_choice = 0;
        public int ModEggs_choice { get => _modEggs_choice; set { SetProperty(ref _modEggs_choice, value); needsSave = true; } }
        private ObservableCollection<string> _modEggs_cln = new ObservableCollection<string>() { "EGM Easter Eggs on (default)", "EGM Easter Eggs off" };
        public ObservableCollection<string> ModEggs_cln { get => _modEggs_cln; }
        private const string ModEggs_TITLE = "EGM Easter Eggs";
        private const string ModEggs_TXT = "EGM has added a few hidden easter eggs, and other things that are purely their for fun, even if they break the fourth wall.\n\nIf you want to avoid anything that might break immersion, switch these off.";

        private const string ModReset_TITLE = "Reset All Settings";
        private const string ModReset_TXT = "This will reset all settings back to the original EGM defaults.";

        private int _modalot_choice = 1;
        public int ModALOT_choice { get => _modalot_choice; set { SetProperty(ref _modalot_choice, value); needsSave = true; } }
        private ObservableCollection<string> _modalot_cln = new ObservableCollection<string>() { "Normal", "Aggressive (Using ALOT) (default)" };
        public ObservableCollection<string> ModALOT_cln { get => _modalot_cln; }
        private const string ModALOT_TITLE = "Using Aggressive Memory Management (for ALOT)";
        private const string ModALOT_TXT = "Mass Effect 3 is limited by its 32bit executable to address only 4GB of memory, which means it has about 3.5GB of space to load textures, models and animations.\n\n" +
            "Large texture mods such as ALOT or armor mods use lots of spare memory (a 4k texture takes about 16x the memory of a 1k one). In addition EGM adds a great deal of new content, such as visitors, animations, vehicles and other items to the Normandy.\n\n" +
            "This setting makes memory management more aggressive (it unloads and reloads more information).  Apply it if you have ALOT installed or multiple armor mods in addition to EGM.\n\n" +
            "It helps with crashes in the armor locker especially. The locker has a bug in its code that means its memory management is particularly poor. " +
            "Slight delays or loading screens maybe noticeable when accessing the armor locker. Using this setting is no guarantee, particularly if you have many armor or casualwear mods that are not optimised to reduce memory usage.";

        private const string ModExport_TITLE = "Export Settings";
        private const string ModExport_TXT = "Export the settings to a backup text file.";
        private const string ModImport_TITLE = "Import Settings";
        private const string ModImport_TXT = "Import the settings from a backup.";
        #endregion

        #region NormandyVars
        private int _norScanner_choice = 0;
        public int NorScanner_choice { get => _norScanner_choice; set { SetProperty(ref _norScanner_choice, value); needsSave = true; } }
        private ObservableCollection<string> _norScanner_cln = new ObservableCollection<string>() { "Walk through (about 0.75secs)", "Sprint through (no pause)", "Full scan (about 4 secs)" };
        public ObservableCollection<string> NorScanner_cln { get => _norScanner_cln; }
        private const string NorScanner_TITLE = "Normandy Security Scanner";
        private const string NorScanner_TXT = "Select how fast the security scanner scans Shepard:\n\n- Walk through (EGM Default): Shepard can walk through with a split second delay.\n- Sprint through: Player ignores the scanner and passes through with no delay.\n- Full Scan (ME3 Default): Player stops for 4 seconds.\n\n";
        private int _norDocking_choice = 1;
        public int NorDocking_choice { get => _norDocking_choice; set { SetProperty(ref _norDocking_choice, value); needsSave = true; } }
        private ObservableCollection<string> _norDocking_cln = new ObservableCollection<string>() { "Galaxy Map (ME3 default)", "Cockpit Door (EGM default)" };
        public ObservableCollection<string> NorDocking_cln { get => _norDocking_cln; }
        private const string NorDocking_TITLE = "Citadel Docking";
        private const string NorDocking_TXT = "Select when Normandy is docked (i.e. at the Citadel), enter via the cockpit airlock (like ME1), or straight to the galaxy map (like ME2).";
        private int _norRelay_choice = 0; //THIS MUST BE PUSHED UP BY +1
        public int NorRelay_choice { get => _norRelay_choice; set { SetProperty(ref _norRelay_choice, value); needsSave = true; } }
        private ObservableCollection<string> _norRelay_cln = new ObservableCollection<string>() { "EGM default Relay video (short)", "ME3 default with lower volume (longer)", "No video (maybe glitches)" };
        public ObservableCollection<string> NorRelay_cln { get => _norRelay_cln; }
        private const string NorRelay_TITLE = "Relay Video";
        private const string NorRelay_TXT = "Select which Relay video to use when transiting clusters on the galaxy map.\n\nNote if you select the No video option there maybe slight graphical glitches that the transition is designed to hide.\n\nThe first relay transition you do in the game will run the full version.  After that all relay transitions will be per your setting.";
        private int _norArm_choice = 1;
        public int NorArm_choice { get => _norArm_choice; set { SetProperty(ref _norArm_choice, value); needsSave = true; } }
        private ObservableCollection<string> _norArm_cln = new ObservableCollection<string>() { "Weapons & Squad selection only", "Armor, Weapons & Squad selection" };
        public ObservableCollection<string> NorArm_cln { get => _norArm_cln; }
        private const string NorArm_TITLE = "Armor Selection on Mission Launch";
        private const string NorArm_TXT = "When launching a combat mission from the Normandy choose armor as well as weapons and squadmates.\n\nThe EGM default is to be able to not only select powers, squadmates and weapons, but also select the appropriate armor for the mission.";
        private int _norStereo_choice = 1;
        public int NorStereo_choice { get => _norStereo_choice; set { SetProperty(ref _norStereo_choice, value); needsSave = true; } }
        private ObservableCollection<string> _norStereo_cln = new ObservableCollection<string>() { "Stereo disabled", "Normandy Stereo enabled" };
        public ObservableCollection<string> NorStereo_cln { get => _norStereo_cln; }
        private const string NorStereo_TITLE = "Normandy Stereo";
        private const string NorStereo_TXT = "Enable or disable the stereo.  There are switches on each deck.\n\nMore tunes are available as you find them in the Citadel DLC.";
        private int _norCabinMus_choice = 1;
        public int NorCabinMus_choice { get => _norCabinMus_choice; set { SetProperty(ref _norCabinMus_choice, value); needsSave = true; } }
        private ObservableCollection<string> _norCabinMus_cln = new ObservableCollection<string>() { "Cabin Music Player", "Normandy Stereo" };
        public ObservableCollection<string> NorCabinMus_cln { get => _norCabinMus_cln; }
        private const string NorCabinMus_TITLE = "Music during Cabin Invites";
        private const string NorCabinMus_TXT = "When your love interest is invited up to the cabin the stereo will automatically start.  It will automatically stop when you exit the cabin.\n\nConfirm which music player to use: the Normandy stereo or the default Cabin Music Player.\n\nNote: If you have the Better Cabin Music Mod then switch to the Cabin player to hear that mod's music instead.";
        private int _norRadio_choice = 1;
        public int NorRadio_choice { get => _norRadio_choice; set { SetProperty(ref _norRadio_choice, value); needsSave = true; norRadioCabin_cb.IsEnabled = value == 1; norRadioGM_cb.IsEnabled = value == 1; if (value == 0) { NorRadioCabin_choice = 0; NorRadioGM_choice = 0; } } }
        private ObservableCollection<string> _norRadio_cln = new ObservableCollection<string>() { "No Background Music (Radio Disabled)", "Normandy Radio Enabled" };
        public ObservableCollection<string> NorRadio_cln { get => _norRadio_cln; }
        private const string NorRadio_TITLE = "Normandy Background Music";
        private const string NorRadio_TXT = "There is a radio on the Normandy, with controls on every deck. You can switch off or select from playlists, with the Normandy SR1 Background music playing by default.\n\n" +
            "Will automatically lower volume or mute during conversations. More playlists can be found in game.";
        private int _norRadioCabin_choice = 1;
        public int NorRadioCabin_choice { get => _norRadioCabin_choice; set { SetProperty(ref _norRadioCabin_choice, value); needsSave = true; } }
        private ObservableCollection<string> _norRadioCabin_cln = new ObservableCollection<string>() { "Cabin Music Player (ME3 Default)", "Normandy Radio in Cabin" };
        public ObservableCollection<string> NorRadioCabin_cln { get => _norRadioCabin_cln; }
        private const string NorRadioCabin_TITLE = "Normandy Radio: Cabin";
        private const string NorRadioCabin_TXT = "Decide whether to use the Normandy Radio in the cabin or the default music player.";
        private int _norRadioGM_choice = 0;
        public int NorRadioGM_choice { get => _norRadioGM_choice; set { SetProperty(ref _norRadioGM_choice, value); needsSave = true; } }
        private ObservableCollection<string> _norRadioGM_cln = new ObservableCollection<string>() { "Galaxy Map Music (ME3 Default)", "Normandy Radio in Map" };
        public ObservableCollection<string> NorRadioGM_cln { get => _norRadioGM_cln; }
        private const string NorRadioGM_TITLE = "Normandy Radio: Galaxy Map";
        private const string NorRadioGM_TXT = "Choose between the default map music and the Normandy Radio to play whilst exploring the galaxy.";
        private int _gmReapers_choice = 0;
        public int GMReapers_choice { get => _gmReapers_choice; set { SetProperty(ref _gmReapers_choice, value); needsSave = true; } }
        private const string GMReapers_TXT = "Once a cluster has fallen to the Reapers, as you search the Galaxy Map for survivors of their attacks, Reapers will hunt you if you make too much noise.\n\nSwitching this off disables galaxy map Reapers in most but not all scenarios." +
            " Relevant assignments will have Reapers even if you switch off. You will have to find a way to evade them.";
        private ObservableCollection<string> _gmReapers_cln = new ObservableCollection<string>() { "Hunted by Reapers", "No Reapers" };
        public ObservableCollection<string> GMReapers_cln { get => _gmReapers_cln; }
        private const string GMReapers_TITLE = "Galaxy Map Reapers";
        private int _gmDisplayGAW_choice = 0;
        public int GMDisplayGAW_choice { get => _gmDisplayGAW_choice; set { SetProperty(ref _gmDisplayGAW_choice, value); needsSave = true; } }
        private const string GMDisplayGAW_TXT = "This changes the behaviour of the % show next to Clusters in the galaxy map.\n\n" +
            "Show GAW % once searched (Default ME3): once any GAW is scanned in that cluster the % shows, but not before.\n\n" +
            "Clusters always display GAW %:  Clusters always show %. A cluster not yet searched but with assets currently available will be at 0%. A cluster with no assets to find currently will be at 100%.";
        private ObservableCollection<string> _gmDisplayGAW_cln = new ObservableCollection<string>() { "Clusters show GAW % once searched", "Clusters always display GAW %" };
        public ObservableCollection<string> GMDisplayGAW_cln { get => _gmDisplayGAW_cln; }
        private const string GMDisplayGAW_TITLE = "Search and Rescue %";
        private int _gmDisplayFuel_choice = 0;
        public int GMDisplayFuel_choice { get => _gmDisplayFuel_choice; set { SetProperty(ref _gmDisplayFuel_choice, value); needsSave = true; } }
        private const string GMDisplayFuel_TXT = "This changes the behaviour of how destroyed fuel depots work with the GAW % exploration in the galaxy map.\n\n" +
            "Show Fuel GAW % (Default ME3): Destroyed Fuel Depots that have not been recovered are included in exploration %.\n\n" +
            "Hide Fuel GAW %: Destroyed Fuel Depots are not included in exploration %.\n\n" +
            "With Hide Fuel % a cluster or system at 100% will have no war assets or salvage to find, but may or may not have fuel.";
        private ObservableCollection<string> _gmDisplayFuel_cln = new ObservableCollection<string>() { "Show Destroyed Fuel Depots GAW %", "Hide Destroyed Fuel Depots GAW %" };
        public ObservableCollection<string> GMDisplayFuel_cln { get => _gmDisplayFuel_cln; }
        private const string GMDisplayFuel_TITLE = "Destroyed Fuel Depots GAW %";

        private int _gmIcons_choice = 0;
        public int GMIcons_choice { get => _gmIcons_choice; set { SetProperty(ref _gmIcons_choice, value); needsSave = true; } }
        private ObservableCollection<string> _gmIcons_cln = new ObservableCollection<string>() { "Fleet Icons On", "Fleet Icons Off" };
        public ObservableCollection<string> GMIcons_cln { get => _gmIcons_cln; }
        private const string GMIcons_TITLE = "Fleet Icons";
        private const string GMIcons_TXT = "EGM adds icons to the galaxy map to represent major forces locations, including the Alliance, Asari, Cerberus, Turian and Citadel Fleets.\n\nTurn this off to remove these icons.";

        private int _norMailSort_choice = 1;
        public int NorMailSort_choice { get => _norMailSort_choice; set { SetProperty(ref _norMailSort_choice, value); needsSave = true; } }
        private ObservableCollection<string> _norMailSort_cln = new ObservableCollection<string>() { "Normandy Mails unsorted", "Normandy Mails sorted (EGM default)" };
        public ObservableCollection<string> NorMailSort_cln { get => _norMailSort_cln; }
        private const string NorMailSort_TITLE = "Normandy Mail Sort";
        private const string NorMailSort_TXT = "Set Normandy Mails to Sort on New v Read then by the Surname of the sender. Honorifics like military titles are ignored. Off will result in the mails unsorted, just depending on what order they are set in the game code.";
        #endregion

        #region MissionVars
        public const string Mission_TITLE = "Mission Timings";
        public const string Mission_TXT = "Use these options to customise when missions become available. If you want to wait to do a DLC or mission, using these options will prevent the mission being given until you decide.\n\nIf you set it to an already completed period it will usually fire immediately. Once you have received a mission, these settings have no effect.\n\nThese settings are designed so entire DLC mission arcs can be completed without having Shepard being told they must be somewhere immediately.";
        private int _FromAshesDLC_choice = 0;
        public int FromAshesDLC_choice { get => _FromAshesDLC_choice; set { SetProperty(ref _FromAshesDLC_choice, value); needsSave = true; } }
        private ObservableCollection<string> _fromAshesDLC_cln = new ObservableCollection<string>() { "Post Mars (default)", "Post Palaven", "Post Surkesh", "Post Tuchanka", "Post Coup", "Disable DLC" };
        public ObservableCollection<string> FromAshesDLC_cln { get => _fromAshesDLC_cln; }
        private const string FromAshesDLC_TITLE = "From Ashes DLC";
        private const string FromAshesDLC_TXT = "Set this to determine when you get an email from Admiral Hackett about a potential opportunity.\n\nUnlocks mission Priority: Eden Prime";
        private int _LeviathanDLC_choice = 0;
        public int LeviathanDLC_choice { get => _LeviathanDLC_choice; set { SetProperty(ref _LeviathanDLC_choice, value); needsSave = true; } }
        private ObservableCollection<string> leviathanDLC_cln = new ObservableCollection<string>() { "Post Palaven (default)", "Post Surkesh", "Post Tuchanka", "Post Geth Dreadnought", "Post Thessia", "Disable DLC" };
        public ObservableCollection<string> LeviathanDLC_cln { get => leviathanDLC_cln; }
        private const string LeviathanDLC_TITLE = "Leviathan DLC";
        private const string LeviathanDLC_TXT = "Set this to determine when you get an email Admiral Hackett about a scientist on the Citadel.\n \nUnlocks Dr Bryson's Laboratory on the Citadel.";
        private int _OmegaDLC_choice = 0;
        public int OmegaDLC_choice { get => _OmegaDLC_choice; set { SetProperty(ref _OmegaDLC_choice, value); needsSave = true; } }
        private ObservableCollection<string> _omegaDLC_cln = new ObservableCollection<string>() { "Post Palaven (default)", "Post Surkesh", "Post Citadel Coup", "Post Geth Dreadnought", "Post Thessia", "Disable DLC" };
        public ObservableCollection<string> OmegaDLC_cln { get => _omegaDLC_cln; }
        private const string OmegaDLC_TITLE = "Omega DLC";
        private const string OmegaDLC_TXT = "Set this to determine when you get an email from Aria inviting you to meet her on the Citadel at Dock 42.\n\nIn addition you must have met with her in the Purgatory nightclub on the Citadel at least once.\n\nUnlocks Dock 42 on the Citadel.";
        private int _CitadelDLC_choice = 0;
        public int CitadelDLC_choice { get => _CitadelDLC_choice; set { SetProperty(ref _CitadelDLC_choice, value); needsSave = true; } }
        private ObservableCollection<string> _citadelDLC_cln = new ObservableCollection<string>() { "Post Coup (default)", "Post Coup + 2 missions", "Post Rannoch", "Post Thessia", "Post Horizon", "Disable DLC (or CEM Full)" };
        public ObservableCollection<string> CitadelDLC_cln { get => _citadelDLC_cln; }
        private const string CitadelDLC_TITLE = "Citadel DLC";
        private const string CitadelDLC_TXT = "Set this to determine when you get an email Admiral Hackett about shore leave.\n\nUnlocks Citadel: Shore Leave.\n\nUsers of the full version of Citadel Epilogue Mod (CEM) should set it this to Disable DLC.";
        private int _prtyTuchanka_choice = 0;
        public int PrtyTuchanka_choice { get => _prtyTuchanka_choice; set { SetProperty(ref _prtyTuchanka_choice, value); needsSave = true; } }
        private ObservableCollection<string> _prtyTuchanka_cln = new ObservableCollection<string>() { "First mission completion (default)", "Both missions completed", "Only Tuchanka: Rescue", "Only Attican Traverse: Krogan Team", "Press button in War Room" };
        public ObservableCollection<string> PrtyTuchanka_cln { get => _prtyTuchanka_cln; }
        private const string PrtyTuchanka_TITLE = "Priority: Tuchanka (Cure for the Genophage)";
        private const string PrtyTuchanka_TXT = "This setting will determine when Mordin says he has synthesised a cure. This is usually given after the end of certain missions given by Wrex and the Primarch:\nTuchanka: Rescue\nAttican Traverse: Krogan Team\n\nIf you choose the final option a button near Turian leader in the war room marked 'Call meeting on cure' will appear. \n\nUnlocks Priority: Tuchanka";
        private int _prtyPerseus_choice = 0;
        public int PrtyPerseus_choice { get => _prtyPerseus_choice; set { SetProperty(ref _prtyPerseus_choice, value); needsSave = true; } }
        private ObservableCollection<string> _prtyPerseus_cln = new ObservableCollection<string>() { "During Coup debrief (default)", "Via vid-con post Coup (at your leisure)" };
        public ObservableCollection<string> PrtyPerseus_cln { get => _prtyPerseus_cln; }
        private const string PrtyPerseus_TITLE = "Priority: Perseus Veil (Meet the Quarians)";
        private const string PrtyPerseus_TXT = "This runs a video conference with Hackett - Post Coup discussion on Quarians.\n\nBy default this is given as part of the post coup debrief.\n\nIf you choose the alternative option a button in the video conference room will appear. You can use it to connect to Hackett whenever you want.\n\nUnlocks Priority: Perseus Veil";
        private int _prtyThessia_choice = 0;
        public int PrtyThessia_choice { get => _prtyThessia_choice; set { SetProperty(ref _prtyThessia_choice, value); needsSave = true; } }
        private ObservableCollection<string> _prtyThessia_cln = new ObservableCollection<string>() { "During Rannoch debrief (default)", "Via vid-con post Rannoch (at your leisure)" };
        public ObservableCollection<string> PrtyThessia_cln { get => _prtyThessia_cln; }
        private const string PrtyThessia_TITLE = "Priority: Citadel III (Meet the Asari Ambassador)";
        private const string PrtyThessia_TXT = "The default start of Citadel III (which leads onto Priority: Thessia) is a video conference with the Asari Councillor post Rannoch.\n\nIf you choose the alternative option a button in the video conference room will appear. You can use it to connect to the Asari Councillor whenever you want.\n\nUnlocks Priority: Citadel III";
        private int _n7Lab_choice = 0;
        public int N7Lab_choice { get => _n7Lab_choice; set { SetProperty(ref _n7Lab_choice, value); needsSave = true; } }
        private ObservableCollection<string> _n7Lab_cln = new ObservableCollection<string>() { "Post Citadel I (default)", "Post Palaven", "Post Surkesh", "Post Surkesh + 2 missions", "Post Coup", "Post Thessia" };
        public ObservableCollection<string> N7Lab_cln { get => _n7Lab_cln; }
        private const string N7Lab_TITLE = "N7: Cerberus Lab";
        private const string N7Lab_TXT = "Set when Traynor tells you about this N7 mission on Sanctum.\n\nNOTE: to complete Citadel: Alien Medi-Gel Formula if setting post-Coup or post Thessia the formula will be available from Spectre Requisitions. You must turn it in before completing Priority: Tuchanka.\n\nUnlocks N7: Cerberus Lab";
        private int _n7benning_choice = 0;
        public int N7benning_choice { get => _n7benning_choice; set { SetProperty(ref _n7benning_choice, value); needsSave = true; } }
        private ObservableCollection<string> _n7benning_cln = new ObservableCollection<string>() { "Post first Krogan mission (default)", "Post Palaven", "Post Surkesh", "Post Coup", "Post Rannoch", "Post Horizon" };
        public ObservableCollection<string> N7benning_cln { get => _n7benning_cln; }
        private const string N7benning_TITLE = "N7: Cerberus Abductions";
        private const string N7benning_TXT = "Set when Traynor tells you about this N7 mission on Benning.\n\nNOTE: to complete Benning: Evidence if setting post-Coup, Rannoch or Horizon the information will be available from Spectre Requisitions. You must turn it in before completing Priority: Tuchanka.\n\nUnlocks N7: Cerberus Abductions";
        private int _n7tuchanka_choice = 0;
        public int N7tuchanka_choice { get => _n7tuchanka_choice; set { SetProperty(ref _n7tuchanka_choice, value); needsSave = true; } }
        private ObservableCollection<string> _n7tuchanka_cln = new ObservableCollection<string>() { "Post Surkesh (default)", "Post Palaven", "Post Tuchanka: Bomb", "Post Coup", "Post Rannoch", "Post Horizon" };
        public ObservableCollection<string> N7tuchanka_cln { get => _n7tuchanka_cln; }
        private const string N7tuchanka_TITLE = "N7: Cerberus Attack";
        private const string N7tuchanka_TXT = "Set when Traynor tells you about this N7 mission on Tuchanka.\n\nNOTE: to complete Citadel: Improved Power Grid if setting post-Coup, Rannoch or Horizon the schematics will be available from Spectre Requisitions. You must turn it in before completing Priority: Tuchanka.\n\nUnlocks N7: Cerberus Attack";
        private int _n7ontarom_choice = 0;
        public int N7ontarom_choice { get => _n7ontarom_choice; set { SetProperty(ref _n7ontarom_choice, value); needsSave = true; } }
        private ObservableCollection<string> _n7ontarom_cln = new ObservableCollection<string>() { "Post Priority: Tuchanka (EGM default)", "Post Surkesh", "Post Coup", "Post Rannoch", "Post Thessia (ME3 default)", "Post Horizon" };
        public ObservableCollection<string> N7ontarom_cln { get => _n7ontarom_cln; }
        private const string N7ontarom_TITLE = "N7: Communication Hub";
        private const string N7ontarom_TXT = "Set when Traynor tells you about this N7 mission on Ontarom.\n\nNOTE: EGM has a different default setting to the vanilla. We felt that this mission was more appropriate for when Cerberus were at the height of their powers, attacking the Citadel, rather than near the end of the game.\n\nUnlocks N7: Communication Hub";
        private int _n7noveria_choice = 0;
        public int N7noveria_choice { get => _n7noveria_choice; set { SetProperty(ref _n7noveria_choice, value); needsSave = true; } }
        private ObservableCollection<string> _n7noveria_cln = new ObservableCollection<string>() { "Post Priority: Horizon (EGM default)", "Post Palaven", "Post Surkesh", "Post Tuchanka (ME3 default)", "Post Coup", "Post Rannoch" };
        public ObservableCollection<string> N7noveria_cln { get => _n7noveria_cln; }
        private const string N7noveria_TITLE = "N7: Cerberus Fighter Base";
        private const string N7noveria_TXT = "Set when Traynor tells you about this N7 mission on Noveria.\n\nNOTE: EGM has a different default setting to the vanilla. We felt that this mission was more appropriate for when Cerberus were being attacked by the Alliance in the Horse Head Nebula, rather than near the Coup.\n\nUnlocks N7: Cerberus Fighter Base";
        private int _n7kypladon_choice = 0;
        public int N7kypladon_choice { get => _n7kypladon_choice; set { SetProperty(ref _n7kypladon_choice, value); needsSave = true; } }
        private ObservableCollection<string> _n7kypladon_cln = new ObservableCollection<string>() { "Post 3 Geth missions (EGM default)", "Post Surkesh", "Post Coup", "Post Geth Dreadnought (ME3 default)", "Post Rannoch", "Post Horizon" };
        public ObservableCollection<string> N7kypladon_cln { get => _n7kypladon_cln; }
        private const string N7kypladon_TITLE = "N7: Fuel Reactors";
        private const string N7kypladon_TXT = "Set when Traynor tells you about this N7 mission on Cyone.\n\nNOTE: EGM has a slightly different default setting to the vanilla. Post Geth-Dreadnought you are presented with several very urgent missions, so this mission becomes a distraction. With the EGM default it becomes available after completing 3 of:\nPriority: Geth Dreadnought\nRannoch: Admiral\nGeth Fighter Squadrons\nPriority: Rannoch\n\nUnlocks N7: Fuel Reactors";

        #endregion

        #region SquadVars
        private int _squad_choice = 0;
        public int Squad_choice { get => _squad_choice; set { SetProperty(ref _squad_choice, value); needsSave = true; } }
        private ObservableCollection<string> _squad_cln = new ObservableCollection<string>() { "Story Mode (default)", "Non-Story Mode (all available)" };
        public ObservableCollection<string> Squad_cln { get => _squad_cln; }
        public const string Squad_StoryMode = "WREX:  Between Priority: Surkesh and Priority: Tuchanka   (will not be available for Rescue/Bomb missions)\nJACK:  For up to 2 missions past Grissom Academy (assuming you don't drop her/students off at the Citadel) then post meeting in Purgatory.\nMIRANDA:  Post Horizon  (requires Miranda Mod add-on)\nJACOB:  After saving on Gellix and speaking in Huerta Memorial Hospital\nSAMARA:  After saving in Mesana and speaking in the Citadel Embassy.\nGRUNT:  After completing the Rachni mission and getting rid of C-SEC on the Citadel.\nKASUMI: Post recruiting during Hanar Diplomat.\nZAEED:  Post recruiting during Volus Ambassador plot (need to speak afterwards).\nARIA:  REQUIRES OMEGA DLC.  Once Omega is completed and only if Shepard proved their complete loyalty.";
        public const string Squad_Notes = "- All bonus squadmates are only available if the Citadel DLC is installed. Aria requires the Omega DLC.\n- Only certain maps have been unlocked for use with the extra squadmates.\n- Default unlocked maps: N7: Labs (Sanctum), N7: Tuchanka, N7: Ontarom, N7: Benning, N7: Noveria, N7: Cyone\n- Additional maps can be found in a seperate add-on pack available on Nexus.\n- Even if a squadmate is set to be available they will appear greyed out and unselectable in the GUI if you try to take them to a mission for which the map is not unlocked.\n- If you take one regular squadmate and one new, you will get all the usual squad chatter (the regular squadmate will speak extra lines).";
        public const string Squad_Notes_LE = "- All bonus squadmates are only available if the Citadel DLC is installed. Aria requires the Omega DLC.\n- Only certain maps have been unlocked for use with the extra squadmates.\n- Default unlocked maps: N7: Labs (Sanctum), N7: Tuchanka, N7: Ontarom, N7: Benning, N7: Noveria, N7: Cyone\n- Even if a squadmate is set to be available they will appear greyed out and unselectable in the GUI if you try to take them to a mission for which the map is not unlocked.\n- If you take one regular squadmate and one new, you will get all the usual squad chatter (the regular squadmate will speak extra lines).";
        private int _squadDisable_choice = 0;
        public int SquadDisable_choice { get => _squadDisable_choice; set { SetProperty(ref _squadDisable_choice, value); needsSave = true; SqdChoice_cb.IsEnabled = value == 0; } }
        private ObservableCollection<string> _squadDisable_cln = new ObservableCollection<string>() { "Enable Extra Squadmates", "Disable Extra Squadmates" };
        public ObservableCollection<string> SquadDisable_cln { get => _squadDisable_cln; }
        public const string SquadDisable_TXT = "You can completely disable the extra squadmates so they will never appear, even if they could be available.";
        #endregion

        #region OutfitVars
        private int _armAlliance_choice = 1;
        public int ArmAlliance_choice { get => _armAlliance_choice; set { SetProperty(ref _armAlliance_choice, value); needsSave = true; } }
        private ObservableCollection<string> _armAlliance_cln = new ObservableCollection<string>() { "Hide all", "Show all (default)" };
        public ObservableCollection<string> ArmAlliance_cln { get => _armAlliance_cln; }
        private const string N7armAlliance_TITLE = "Alliance Standard Armors";
        private const string N7armAlliance_TXT = "Show extra Alliance standard armors in the armor locker:\n\nFull body: Special Forces Heavy, Special Forces Medium\nTorso: Marine Officer (tintable), Phoenix (Femshep), Marine (Maleshep)\nHelmet: Standard issue breather";
        private int _ArmAPP_choice = 0;
        public int ArmAPP_choice { get => _ArmAPP_choice; set { SetProperty(ref _ArmAPP_choice, value); needsSave = true; } }
        private ObservableCollection<string> _ArmAPP_cln = new ObservableCollection<string>() { "Is not installed (default)", "Alternative Appearance Pack is installed" };
        public ObservableCollection<string> ArmAPP_cln { get => _ArmAPP_cln; }
        private const string N7ArmAPP_TITLE = "Cerberus Ajax Armor";
        private const string N7ArmAPP_TXT = "Unlocks the torso and helmet as seperate items, and makes the armor appear.\n\nWARNING: IF YOU UNLOCK THIS WITHOUT HAVING BIOWARE'S ALTERNATIVE APPEARANCE PACK DLC IT WILL BREAK THE ARMOR LOCKER.";
        private int _casGarrus_choice = 1;
        public int CasGarrus_choice { get => _casGarrus_choice; set { SetProperty(ref _casGarrus_choice, value); needsSave = true; } }
        private ObservableCollection<string> _casGarrus_cln = new ObservableCollection<string>() { "Armored Garrus (same as mission)", "Default Blue (ME3 default)", "Gold Trim Armor", "Desert Camouflage (From Ashes)", "Archangel Terminus (Alternative Appearance Pack)", "Gold Trim Casual (EGM default)", "C-SEC (ME1)", "Formalwear (EGM Squadmate Pack)"  };
        public ObservableCollection<string> CasGarrus_cln { get => _casGarrus_cln; }
        private const string N7casgarrus_TITLE = "Casual Garrus";
        private const string N7casgarrus_TXT = "When on the Normandy and Citadel Presidium Garrus will wear his selected casual outfit. Camo and Terminus require Custom Extras Pack and From Ashes DLC/Alternate Appearance DLC.\n\n" +
            "Between the Citadel DLC missions he will wear armor, whichever armor he wears during the missions. For the party he can be casual. He will always wear either casual or formal if invited to the cabin when romanced.\n";
        private int _casedi_choice = 0;
        public int CasEDI_choice { get => _casedi_choice; set { SetProperty(ref _casedi_choice, value); needsSave = true; } }
        private ObservableCollection<string> _casedi_cln = new ObservableCollection<string>() { "Metal (default)", "Black/Blue Leather", "Alliance Leather (From Ashes DLC)", "Alternative Armor (APP DLC)", "Formal Outfit (EGM Squadmate Pack)", "Synth Skin", "Automatic (same as armor)" };
        public ObservableCollection<string> CasEDI_cln { get => _casedi_cln; }
        private const string N7casedi_TITLE = "Casual EDI";
        private const string N7casedi_TXT = "Alliance Leather requires EGM Custom Extras Pack and From Ashes.\nAlternative Armor requires Extra and Alternate Appearance DLC." +
            "\nThe formal outfit was made for the Citadel DLC Casino mission but never used. Requires EGM Squadmate Pack and the Citadel DLC." +
            "\nSynth skin is the infiltration skin employed by Cerberus, the formula must be found in game for it to be used.\nAutomatic is the same as current squad combat selection." +
            "\n\nEDI will always have the metal body during the reveal scene. During the Citadel DLC missions EDI will always be in the same armor as selected for missions.";
        private int _casliara_choice = 0;
        public int CasLiara_choice { get => _casliara_choice; set { SetProperty(ref _casliara_choice, value); needsSave = true; } }
        private ObservableCollection<string> _casliara_cln = new ObservableCollection<string>() { "Shadow Broker (default)", "Scientist (ME1)", "Pink Broker (CE)", "Alternative Armor (APP DLC)" };
        public ObservableCollection<string> CasLiara_cln { get => _casliara_cln; }
        private const string N7casliara_TITLE = "Casual Liara";
        private const string N7casliara_TXT = "Pink Broker requires EGM Custom Extras Pack and the collectors edition. The Alternative Armor requires a custom extra and the Alternate Appearance Pack DLC.\n\n" +
            "Scientist is the same outfit as she wore in ME1.\n\nDuring the Citadel DLC missions Liara will always be in the same armor as selected for missions, if it is available as a casual option or formalwear if taken on the Casino mission.";
        private int _casash_choice = 0;
        public int CasAsh_choice { get => _casash_choice; set { SetProperty(ref _casash_choice, value); needsSave = true; } }
        private ObservableCollection<string> _casash_cln = new ObservableCollection<string>() { "Blue S1 (default)", "Naval Regs", "Pink S1 (CE)", "Dress Blues"};
        public ObservableCollection<string> CasAsh_cln { get => _casash_cln; }
        private const string N7casash_TITLE = "Casual Ashley";
        private const string N7casash_TXT = "Note: Ashley will wear the selection during the prologue.\n\nPink S1 requires collectors edition and the EGM Custom Extras Pack.\n\n" +
            "Between the Citadel DLC missions Ashley will always be in the same armor as selected for missions, if it is available as a casual option or formalwear if she went to the Casino.\n";
        private int _castali_choice = 0;
        public int CasTali_choice { get => _castali_choice; set { SetProperty(ref _castali_choice, value); needsSave = true; } }
        private ObservableCollection<string> _castali_cln = new ObservableCollection<string>() { "Standard (default)", "Gold Trims", "Faceplate (From Ashes)", "Hood Down (EGM Squadmate Pack)", "Citadel Formalwear (EGM Squadmate Pack)", "Automatic (same as mission)" };
        public ObservableCollection<string> CasTali_cln { get => _castali_cln; }
        private const string N7castali_TITLE = "Casual Tali";
        private const string N7castali_TXT = "Automatic option will select whichever armor is used for missions. Faceplate requires Casual EGM Custom Extra and the From Ashes DLC.\n\nHood Down and Citadel Formalwear require the EGM Squadmate pack and the Citadel DLC.\n\n" +
            "In the apartment during the Citadel DLC missions Tali will always be in the same armor as selected for missions, or the formal wear if she has been to the Casino.";
        private int _caskai_choice = 0;
        public int CasKai_choice { get => _caskai_choice; set { SetProperty(ref _caskai_choice, value); needsSave = true; } }
        private ObservableCollection<string> _caskai_cln = new ObservableCollection<string>() { "Naval regs (default)", "Alternate regs", "Dress Blues" };
        public ObservableCollection<string> CasKai_cln { get => _caskai_cln; }
        private const string N7caskai_TITLE = "Casual Kaidan";
        private const string N7caskai_TXT = "Note: Kaidan will wear the selection during the prologue.\n\nAlternate regs are the same as used by Shepard during the prologue.";
        private int _casjav_choice = 0;
        public int CasJav_choice { get => _casjav_choice; set { SetProperty(ref _casjav_choice, value); needsSave = true; } }
        private ObservableCollection<string> _casjav_cln = new ObservableCollection<string>() { "Red armor (default)", "Black armor (From Ashes)", "Red Casual (From Ashes)", "Black Casual (From Ashes)" };
        public ObservableCollection<string> CasJav_cln { get => _casjav_cln; }
        private const string N7casjav_TITLE = "Casual Javik";
        private const string N7casjav_TXT = "Javik will always be found in his red armor on Eden Prime. He will change on boarding the Normandy.\n\nBlack armor and both Casuals requires EGM Custom Extras Pack and the From Ashes DLC.\n\n" +
            "During the Citadel DLC missions Javik will always be in the same armor as selected for missions, if it is available as a casual option.\n\nThanks to Scottina for the Casual Javik outfit.";
        private int _casvega_choice = 0;
        public int CasVega_choice { get => _casvega_choice; set { SetProperty(ref _casvega_choice, value); needsSave = true; } }
        private ObservableCollection<string> _casvega_cln = new ObservableCollection<string>() { "Marine T-Shirt (default)", "Naval regulation outfit", "Dress Blues" };
        public ObservableCollection<string> CasVega_cln { get => _casvega_cln; }
        private const string N7casvega_TITLE = "Casual Vega";
        private const string N7casvega_TXT = "Vega will wear the selection on the Normandy and Citadel, but not in the prologue.\n\n" +
            "When invited to the apartment he will wear his T-shirt.";
        private int _casmirry_choice = 0;
        public int CasMirry_choice { get => _casmirry_choice; set { SetProperty(ref _casmirry_choice, value); needsSave = true; } }
        private ObservableCollection<string> _casmirry_cln = new ObservableCollection<string>() { "White Catsuit (default)", "Black Catsuit", "Black Armor (AAP)", "Formalwear Red dress" };
        public ObservableCollection<string> CasMirry_cln { get => _casmirry_cln; }
        private const string N7casmirry_TITLE = "Casual Miranda (requires Miranda Mod)";
        private const string N7casmirry_TXT = "Miranda will wear this casual throughout the game (requires Miranda Mod with Miranda's casuals installed).  Note other mods may override this.";
        #endregion

        #region MiscVars
        private int _casmiranda_choice = 0;
        public int CasMiranda_choice { get => _casmiranda_choice; set { SetProperty(ref _casmiranda_choice, value); needsSave = true; } }
        private ObservableCollection<string> _casmiranda_cln = new ObservableCollection<string>() { "White catsuit (default)", "AVPen's Alternative Uniform" };
        public ObservableCollection<string> CasMiranda_cln { get => _casmiranda_cln; }
        private const string N7casmiranda_TITLE = "Casual Miranda (Requires Miranda Mod)";
        private const string N7casmiranda_TXT = "If invited to the Normandy post-Horizon, Miranda can be either in her default white outfit or an alternative casual white uniform.\n\nTextures created AVPen.\n\nRequires Miranda Mod add-on.";

        private int _arkN7Paladin_choice = 0;
        public int ArkN7Paladin_choice { get => _arkN7Paladin_choice; set { SetProperty(ref _arkN7Paladin_choice, value); needsSave = true; } }
        private ObservableCollection<string> _arkN7Paladin_cln = new ObservableCollection<string>() { "Post Coup (default)", "Post Rannoch", "Post Thessia" };
        public ObservableCollection<string> ArkN7Paladin_cln { get => _arkN7Paladin_cln; }
        private const string ArkN7Paladin_TITLE = "N7: Operation Paladin (Requires Ark Mod)";
        private const string ArkN7Paladin_TXT = "Set when this mission begins.\n\nNote the story of this mission takes place during the Miracle of Palaven which happens between the Coup and the end of the Geth missions.\n\nRequires Ark Mod add-on.\n\nUnlocks N7: Operation Paladin";

        private int _arkBenning_choice = 0;
        public int ArkBenning_choice { get => _arkBenning_choice; set { SetProperty(ref _arkBenning_choice, value); needsSave = true; } }
        private ObservableCollection<string> _arkBenning_cln = new ObservableCollection<string>() { "Cerberus Ambush (Hard)", "Cerberus Faction (Normal)" };
        public ObservableCollection<string> ArkBenning_cln { get => _arkBenning_cln; }
        private const string ArkBenning_TITLE = "N7: Cerberus Abductions Difficulty";
        private const string ArkBenning_TXT = "The mission N7: Cerberus Abductions has a special difficulty setting for the Acid Rain hazard map. Setting at the Cerberus Ambush level will mean:\n\n" +
            "(1) Higher level Dragoons will join the ambush.\n(2) You will need to keep close to the civilians you are accompaning. If you stray too far for too long they will die." +
            "\n\nThis is in addition to the Acid Rain Hazard. Set before leaving the Normandy.  If you want to change this setting during the mission, set and then reload from the Chapter Save.";

        private int _armShp_choice = 0;
        public int ArkVsr_choice { get => _armShp_choice; set { SetProperty(ref _armShp_choice, value); needsSave = true; } }
        private ObservableCollection<string> _armShp_cln = new ObservableCollection<string>() { "Select Visor Opacity...", "Transparent", "Opaque" };
        public ObservableCollection<string> ArkVsr_cln { get => _armShp_cln; }
        private const string ArkVsr_TITLE = "Helmet Visor Selection";
        private const string ArkVsr_TXT = "Our artist Furinax recreated and rigged the armors from Mass Effect Andromeda into Mass Effect 3.  He always preferred an opaque visor look, whilst Mass Effect has traditionally had transparent visors so the player can see Shepard's face.  Luckily you can now choose:\n\n" +
            "Transparent - the helmets in Ark Mod have tinted transparent visors so Shepard's face will be visible in conversations.\n\n" +
            "Opaque - the helmets in Ark Mod have the artist's original vision of opaque visors." +
            "\n\nNote - this can be changed after ALOT is installed. Ark will swap existing files and then generate new toc files. Exit to desktop and restart ME3.";

        private int _armWreck_choice = 0;
        public int ArmWreck_choice { get => _armWreck_choice; set { SetProperty(ref _armWreck_choice, value); needsSave = true; } }
        private ObservableCollection<string> _armWreck_cln = new ObservableCollection<string>() { "Find Immersively in Mission", "Automatically added to Armor Locker" };
        public ObservableCollection<string> ArmWreck_cln { get => _armWreck_cln; }
        private const string ArmWreck_TITLE = "EGM Armors - Find some armors during mission";
        private const string ArmWreck_TXT = "Some of the armors related to the Andromeda Initiative can be found immersively during a mission. Impacts EGM Armors for LE3 only, and will only take effect if certain other mods are installed.";

        #endregion

        #region Initial/Exit
        public SettingsPanel()
        {
            LoadCommands();
            InitializeComponent();
            string[] args = Environment.GetCommandLineArgs();
            string argdir;
            MEGame argmode = MEGame.LE3;
            if (args.Length > 1 && Directory.Exists(argdir = Path.GetFullPath(args[1].Trim('"'))))
            {
                //test if ME3 directory has been passed to the settings
                if(File.Exists(Path.Combine(argdir,"Binaries\\Win32\\MassEffect3.exe")))
                {
                    me3Path = argdir;
                    argmode = MEGame.ME3;
                }
                //test if LE3 directory has been passed to the settings
                if (File.Exists(Path.Combine(argdir, "Binaries\\Win64\\MassEffect3.exe")))
                {
                    le3Path = argdir;
                    argmode = MEGame.LE3;
                }
            }

            ChangeMode(argmode);

        }

        private void ChangeMode(MEGame newGame)
        {
            mode = newGame;
            tab_Diagnostics.Visibility = Visibility.Collapsed;
            currentView = 0;
            var binDir = GetBinDirectory();
            if (binDir != null)
            {
                binPath = Path.Combine(binDir, "EGMSettings.ini");
                verPath = Path.Combine(binDir, "EGMSettingsVersion.ini");
            }

            if (binPath == null || gamePath == null)
            {
                StatusText = "Mass Effect 3 (" + newGame.ToString() + ") install not found. Changes will not be saved";
                status_TxtBx.Foreground = Brushes.Red;
                status_TxtBx.FontWeight = FontWeights.Bold;
                btn_diag.IsEnabled = false;
                return;
            }


            GetEGMSetup();
            CreatingSettings();
            switch (newGame) //Disable certain functions
            {
                case MEGame.LE3:
                    header_TITLE = $"Expanded Galaxy Mod Settings {currentBuild} - Legendary Edition";
                    LogoImg.Source = new BitmapImage(new Uri("EGM_LE_Settings_Splash.png", UriKind.Relative));
                    majMissions_lbl.Text = "Major Missions (Requires Normandy Module)";
                    //Mod
                    warassets_ttl.Visibility = Visibility.Collapsed;
                    modBeta_lbl.Visibility = Visibility.Collapsed;
                    modBeta_cbx.Visibility = Visibility.Collapsed;
                    modQP_lbl.Visibility = Visibility.Collapsed;
                    modQP_cbx.Visibility = Visibility.Collapsed;
                    modAssgn_lbl.IsEnabled = galMap;
                    modAssgn_cbx.IsEnabled = galMap;
                    modEggs_lbl.Visibility = Visibility.Collapsed;
                    modEggs_cbx.Visibility = Visibility.Collapsed;
                    compat_ttl.Visibility = Visibility.Collapsed;
                    modALOT_lbl.Visibility = Visibility.Collapsed;
                    modALOT_cbx.Visibility = Visibility.Collapsed;

                    //Normandy
                    norDock_cb.IsEnabled = normandy;
                    norDock_lbl.IsEnabled = normandy;
                    norStereo_cb.Visibility = Visibility.Collapsed;
                    norStereo_lbl.Visibility = Visibility.Collapsed;
                    norLIMus_cb.Visibility = Visibility.Collapsed;
                    norLIMus_lbl.Visibility = Visibility.Collapsed;
                    norRelay_cb.IsEnabled = galMap;
                    gmReapers_cb.IsEnabled = galMap;
                    gmIcons_cb.IsEnabled = galMap;
                    gmDisplayGAW_cb.IsEnabled = galMap;
                    gmDisplayFuel_cb.IsEnabled = galMap;
                    norLIMus_lbl.IsEnabled = false;
                    norRelay_lbl.IsEnabled = galMap;
                    gmReapers_lbl.IsEnabled = galMap;
                    gmIcons_lbl.IsEnabled = galMap;
                    gmDisplayGAW_lbl.IsEnabled = galMap;
                    gmDisplayFuel_lbl.IsEnabled = galMap;
                    stk_music.Visibility = Visibility.Visible;
                    ttl_music.Visibility = Visibility.Visible;
                    gmDisplayFuel_lbl.Visibility = Visibility.Visible;
                    gmDisplayFuel_cb.Visibility = Visibility.Visible;
                    norRadio_cb.IsEnabled = normandy;
                    norRadio_lbl.IsEnabled = normandy;
                    norRadioCabin_cb.IsEnabled = normandy;
                    norRadioCabin_lbl.IsEnabled = normandy;
                    norRadioGM_cb.IsEnabled = normandy;
                    norRadioGM_lbl.IsEnabled = normandy;
                    norRadio_cb.Visibility = Visibility.Visible;
                    norRadio_lbl.Visibility = Visibility.Visible;
                    norRadioCabin_cb.Visibility = Visibility.Visible;
                    norRadioCabin_lbl.Visibility = Visibility.Visible;
                    norRadioGM_cb.Visibility = Visibility.Visible;
                    norRadioGM_lbl.Visibility = Visibility.Visible;
                    norMail_cb.IsEnabled = normandy;
                    norMail_lbl.IsEnabled = normandy;
                    norMail_cb.Visibility = Visibility.Visible;
                    norMail_lbl.Visibility = Visibility.Visible;
                    //Missions
                    priorTuchanka_cb.IsEnabled = normandy;
                    priorPerseus_cb.IsEnabled = normandy;
                    priorCit3_cb.IsEnabled = normandy;
                    priorTuchanka_lbl.IsEnabled = normandy;
                    priorPerseus_lbl.IsEnabled = normandy;
                    priorCit3_lbl.IsEnabled = normandy;
                    //Outfits
                    tab_outfits.IsEnabled = squadmate;
                    ShepsArmors_ttl.Visibility = Visibility.Collapsed;
                    Armors_grp.Visibility = Visibility.Collapsed;
                    allArmor_cb.IsEnabled = false;
                    allArmor_lbl.IsEnabled = false;
                    AAP_cb.IsEnabled = false;
                    AAP_lbl.IsEnabled = false;
                    casMirry_lbl.Visibility = Visibility.Visible;
                    casMirry_cb.Visibility = Visibility.Visible;
                    casMirry_lbl.IsEnabled = mirandaMod;
                    casMirry_cb.IsEnabled = mirandaMod;
                    //Squad
                    tab_squad.IsEnabled = squadmate;
                    BonusSquad_txt_LE.Visibility = Visibility.Visible;
                    BonusSquad_txt.Visibility = Visibility.Collapsed;
                    SqdDisable_cb.Visibility = Visibility.Visible;
                    SqdChoice_lbl.Visibility = Visibility.Visible;
                    //Tabs
                    tab_misc.IsEnabled = (armorMod && shipwreckMod);
                    casMiranda_ttl.Visibility = Visibility.Collapsed;
                    casMiranda_dck.Visibility = Visibility.Collapsed;
                    arkMod_ttl.Visibility = Visibility.Collapsed;
                    arkMod_dck.Visibility = Visibility.Collapsed;
                    armMod_ttl.Visibility = Visibility.Visible;
                    armMod_dck.Visibility = Visibility.Visible;
                    armMod_cb.IsEnabled = (armorMod && shipwreckMod);
                    break;
                default:
                    header_TITLE = $"Expanded Galaxy Mod Settings {currentBuild} - Original Edition";
                    LogoImg.Source = new BitmapImage(new Uri("EGM_Logo_v3.png", UriKind.Relative));
                    majMissions_lbl.Text = "Major Missions";

                    //Mod
                    warassets_ttl.Visibility = Visibility.Visible;
                    modBeta_lbl.Visibility = Visibility.Visible;
                    modBeta_cbx.Visibility = Visibility.Visible;
                    modQP_lbl.Visibility = Visibility.Visible;
                    modQP_cbx.Visibility = Visibility.Visible;
                    modAssgn_lbl.IsEnabled = true;
                    modAssgn_cbx.IsEnabled = true;
                    modEggs_lbl.Visibility = Visibility.Visible;
                    modEggs_cbx.Visibility = Visibility.Visible;
                    compat_ttl.Visibility = Visibility.Visible;
                    modALOT_lbl.Visibility = Visibility.Visible;
                    modALOT_cbx.Visibility = Visibility.Visible;
                    //Normandy
                    norDock_cb.IsEnabled = true;
                    norDock_lbl.IsEnabled = true;
                    norStereo_cb.Visibility = Visibility.Visible;
                    norStereo_lbl.Visibility = Visibility.Visible;
                    norLIMus_cb.Visibility = Visibility.Visible;
                    norLIMus_lbl.Visibility = Visibility.Visible;
                    norRelay_cb.IsEnabled = true;
                    gmReapers_cb.IsEnabled = true;
                    gmIcons_cb.IsEnabled = true;
                    norLIMus_lbl.IsEnabled = true;
                    norRelay_lbl.IsEnabled = true;
                    gmReapers_lbl.IsEnabled = true;
                    gmIcons_lbl.IsEnabled = true;
                    gmReapers_lbl.IsEnabled = true;
                    gmIcons_lbl.IsEnabled = true;
                    gmDisplayGAW_lbl.IsEnabled = false; //LE3Only
                    gmDisplayGAW_cb.IsEnabled = false;
                    gmDisplayFuel_lbl.IsEnabled = false; //LE3Only
                    gmDisplayFuel_cb.IsEnabled = false;
                    stk_music.Visibility = Visibility.Visible;
                    ttl_music.Visibility = Visibility.Visible;
                    gmDisplayFuel_lbl.Visibility = Visibility.Collapsed;
                    gmDisplayFuel_cb.Visibility = Visibility.Collapsed;
                    norRadio_cb.Visibility = Visibility.Collapsed;
                    norRadio_lbl.Visibility = Visibility.Collapsed;
                    norRadioCabin_cb.Visibility = Visibility.Collapsed;
                    norRadioCabin_lbl.Visibility = Visibility.Collapsed;
                    norRadioGM_cb.Visibility = Visibility.Collapsed;
                    norRadioGM_lbl.Visibility = Visibility.Collapsed;
                    norMail_cb.Visibility = Visibility.Collapsed;
                    norMail_lbl.Visibility = Visibility.Collapsed;
                    //Missions
                    priorTuchanka_cb.IsEnabled = true;
                    priorPerseus_cb.IsEnabled = true;
                    priorCit3_cb.IsEnabled = true;
                    priorTuchanka_lbl.IsEnabled = true;
                    priorPerseus_lbl.IsEnabled = true;
                    priorCit3_lbl.IsEnabled = true;
                    //Outfits
                    ShepsArmors_ttl.Visibility = Visibility.Visible;
                    Armors_grp.Visibility = Visibility.Visible;
                    allArmor_cb.IsEnabled = true;
                    allArmor_lbl.IsEnabled = true;
                    AAP_cb.IsEnabled = true;
                    AAP_lbl.IsEnabled = true;
                    casMirry_lbl.Visibility = Visibility.Collapsed;
                    casMirry_cb.Visibility = Visibility.Collapsed;
                    casMirry_lbl.IsEnabled = false;
                    casMirry_cb.IsEnabled = false;
                    //Squad
                    SqdDisable_cb.Visibility = Visibility.Collapsed;
                    SqdChoice_lbl.Visibility = Visibility.Collapsed;
                    //Tabs
                    tab_outfits.IsEnabled = true;
                    tab_squad.IsEnabled = true;
                    tab_misc.IsEnabled = true;
                    //Ark Mod
                    casMiranda_ttl.Visibility = Visibility.Visible;
                    casMiranda_dck.Visibility = Visibility.Visible;
                    arkMod_ttl.Visibility = Visibility.Visible;
                    arkMod_dck.Visibility = Visibility.Visible;
                    armMod_ttl.Visibility = Visibility.Collapsed;
                    armMod_dck.Visibility = Visibility.Collapsed;
                    if (File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_MOD_EGM_Ark\\CookedPCConsole\\Default.sfar")))
                    {
                        arkvsr_cb.IsEnabled = true;
                        arkvsr_txt.IsEnabled = true;
                    }
                    break;
            }

            //Create version
            if (!File.Exists(verPath))
            {
                UpdateVersion = defUpdateCount;
                SaveSettingsVersion();
            }

            //Load ini or create
            if (!File.Exists(binPath))
            {
                //First time set AAP to correct value
                var aapDir = Directory.GetDirectories(gamePath, "DLC_CON_APP01", SearchOption.AllDirectories).Any();
                if (aapDir)
                {
                    ArmAPP_choice = 1;
                }
                SaveSettings();
                StatusText = "Default EGM settings file created.";
            }
            else
            {
                LoadSettingsVersion();
                LoadSettings();
                StatusText = "Existing EGM settings file loaded.";
            }
        }

        private void SwitchGame()
        {
            ChangeMode(mode == MEGame.ME3 ? MEGame.LE3 : MEGame.ME3);
        }

        private void LoadCommands()
        {
            SaveCommand = new GenericCommand(SaveSettings);
            LoadCommand = new GenericCommand(LoadSettings);
            NextCommand = new GenericCommand(MoveNextTab, CanNextCommand);
            BackCommand = new GenericCommand(MoveBackTab, CanBackCommand);
            FinishCommand = new GenericCommand(FinishSettings);
            ResetDefaultCommand = new GenericCommand(ResetToDefault);
            ShowDiagnosticsCommand = new GenericCommand(ShowDiagnostics);
            AutoTOCCommand = new GenericCommand(GenerateTOCS);
            SwitchGameCommand = new GenericCommand(SwitchGame);
            ExportCommand = new GenericCommand(ExportSettings);
            ImportCommand = new GenericCommand(ImportSettings);
        }

        private void EGMSettings_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void EGMSettings_Closing(object sender, CancelEventArgs e)
        {
            //Check if ini data needs saving
            if(needsSave && binPath != null)
            {
                var xdlg = MessageBox.Show("Changes have not been saved. Save now?", "EGM Settings", MessageBoxButton.YesNo);
                if (xdlg == MessageBoxResult.Yes)
                {
                    SaveSettingsVersion();
                    SaveSettings();
                }
            }
        }
        
        private string GetBinDirectory()
        {
            string basePath = mode == MEGame.LE3 ? le3Path : me3Path ;
            string appPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            if(basePath == null && appPath != null)
            {
                basePath = FindME3ParentDir(appPath);
                if(basePath == null)
                {
                    //get ME3Directory from registry
                    string hkey32 = @"HKEY_LOCAL_MACHINE\SOFTWARE\";
                    string hkey64 = @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\";
                    string subkey = @"BioWare\Mass Effect 3";
                    if (mode == MEGame.LE3) //or LE3Directory from registry
                    {
                        hkey32 = @"HKEY_LOCAL_MACHINE\SOFTWARE\";
                        hkey64 = @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\";
                        subkey = @"BioWare\Mass Effect Legendary Edition";
                    }

                    string keyName = hkey32 + subkey;
                    basePath = (string)Microsoft.Win32.Registry.GetValue(keyName, "Install Dir", null);
                    if (basePath == null)
                    {
                        keyName = hkey64 + subkey;
                        basePath = (string)Microsoft.Win32.Registry.GetValue(keyName, "Install Dir", null);
                    }
                    if (mode == MEGame.LE3)
                        basePath = basePath + "Game\\ME3\\";
                }
            }

            if(basePath != null && Directory.Exists(basePath))
            {
                gamePath = basePath;
                var exePath = Directory.GetFiles(basePath, "MassEffect3.exe", SearchOption.AllDirectories).FirstOrDefault();
                if(exePath == null)
                {
                    return null; //Game exe not found.
                }
                var d = new DirectoryInfo(Path.GetDirectoryName(exePath));
                if (d.Parent.Name == "Binaries")
                    return d.Parent.FullName;
            }

            return null;
        }

        private string FindME3ParentDir(string currPath)
        {
            string me3DirName = mode == MEGame.LE3 ? "ME3" : "Mass Effect 3";
            var dirInfo = new DirectoryInfo(Path.GetDirectoryName(currPath));
            if(dirInfo.Name != me3DirName)
            {
                if (dirInfo.Parent == null)
                    return null;
                if (dirInfo.Parent.Name == me3DirName)
                    return dirInfo.Parent.FullName;
                if (dirInfo.Parent.Parent == null)
                    return null;
                currPath = FindME3ParentDir(dirInfo.Parent.FullName);
            }
            return currPath;
        }

        private void GetEGMSetup()
        {
            egmPath = Path.Combine(gamePath, "BioGame\\DLC\\DLC_MOD_EGM\\");
            var metaFile = Path.Combine(gamePath, "BioGame\\DLC\\DLC_MOD_EGM\\", "_metacmm.txt");
            framework = File.Exists(Path.Combine(gamePath, "BioGame\\DLC\\DLC_MOD_Framework\\CookedPCConsole", "Default_DLC_MOD_Framework.bin"));
            mirandaMod = File.Exists(Path.Combine(gamePath, "BioGame\\DLC\\DLC_MOD_EGM_Miranda\\CookedPCConsole", "Default_DLC_MOD_EGM_Miranda.bin"));
            armorMod = File.Exists(Path.Combine(gamePath, "BioGame\\DLC\\DLC_MOD_EGM_Armors\\CookedPCConsole", "Default_DLC_MOD_EGM_Armors.bin"));
            shipwreckMod = File.Exists(Path.Combine(gamePath, "BioGame\\DLC\\DLC_MOD_EGM_Shipwreck\\CookedPCConsole", "Default_DLC_MOD_EGM_Shipwreck.bin"));
            try
            {
                if (File.Exists(metaFile))
                {
                    egmMetaData = File.ReadAllLines(metaFile);
                    if (egmMetaData != null)
                    {
                        Version version = Version.Parse(egmMetaData[1]);
                        var options = ParseMetaInstallOptions();
                        if (options != null)
                        {
                            squadmate = (options.Contains<string>("Squad: Squadmate Pack (Full)") || options.Contains<string>("Squadmate Pack (Full)"));
                            fixCutscenes = options.Contains<string>("Fix Weapons in Cutscenes");
                            if (options.Contains<string>("Setup: Galactic War ALPHA + Normandy Overhaul + Core") ||
                                options.Contains<string>("Setup: Galactic War + Normandy Overhaul + Core") ||
                                options.Contains<string>("Setup: Galactic War + Normandy Overhaul + Visitors + Core") ||
                                options.Contains<string>("Modules: Galactic War + Normandy Overhaul + Core") ||
                                options.Contains<string>("Modules: Galactic War + Normandy Overhaul + Visitors + Core"))
                            {
                                normandy = true;
                                galMap = true;
                            }
                            else if (options.Contains<string>("Setup: Normandy Overhaul + Visitors + Core") ||
                                options.Contains<string>("Setup: Normandy Overhaul + Core") ||
                                options.Contains<string>("Modules: Normandy Overhaul + Visitors + Core") ||
                                options.Contains<string>("Modules: Normandy Overhaul + Core"))
                            {
                                normandy = true;
                                galMap = false;
                            }
                            else
                            {
                                normandy = false;
                                galMap = false;
                            }
                        }
                    }
                }
            }
            catch 
            {
               //Exception
            }
        }

        private string[] ParseMetaInstallOptions()
        {
            if(egmMetaData != null && egmMetaData[4].Length > 16)
            {
                var installOpt = egmMetaData[4].Remove(0, 16);
                var options = installOpt.Split(';');
                return options;
            }
            return null;
        }

        private void CreatingSettings()
        {
            Settings.Clear();
            if(mode == MEGame.ME3)
            {
                //Mod
                Settings.Add(new ModSetting(29436, "ModWARBeta", false, 0, 1));
                Settings.Add(new ModSetting(29415, "ModQP", true, 0, 0));
                Settings.Add(new ModSetting(29440, "ModAssign", false, 0, 0));
                Settings.Add(new ModSetting(28937, "ModEggs", true, 0, 0));
                Settings.Add(new ModSetting(28855, "ModALOT", true, 1, 0));
                //Nor
                Settings.Add(new ModSetting(29338, "NorDocking", true, 1, 0));
                Settings.Add(new ModSetting(29338, "NorRelay", false, 0, 1));
                Settings.Add(new ModSetting(28857, "NorStereo", true, 1, 0));
                Settings.Add(new ModSetting(28856, "NorCabinMus", true, 1, 0));
                Settings.Add(new ModSetting(28993, "GMReapers", true, 0, 0));
                Settings.Add(new ModSetting(28994, "GMIcons", true, 0, 0));
                //Squad
                Settings.Add(new ModSetting(28750, "Squad", true, 0, 0));
                //Mission Main
                Settings.Add(new ModSetting(29337, "PrtyTuchanka", false, 0, 0));
                Settings.Add(new ModSetting(29335, "PrtyPerseus", false, 0, 0));
                Settings.Add(new ModSetting(29334, "PrtyThessia", false, 0, 0));
                //Outfits
                Settings.Add(new ModSetting(28824, "ArmAlliance", true, 1, 0));
                if (File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_CON_APP01\\CookedPCConsole\\Default.sfar")))
                    Settings.Add(new ModSetting(28819, "ArmAPP", true, 1, 0));
                else
                    Settings.Add(new ModSetting(28819, "ArmAPP", true, 0, 0));
                Settings.Add(new ModSetting(28870, "CasMiranda", true, 0, 0));
                //Mission ArkMod
                Settings.Add(new ModSetting(28650, "ArkN7Paladin", false, 0, 0));
                Settings.Add(new ModSetting(28936, "ArkBenning", true, 0, 0));
                //Outfits
                Settings.Add(new ModSetting(28988, "CasGarrus", false, 5, 0));
                Settings.Add(new ModSetting(28995, "CasEDI", false, 0, 0));
                Settings.Add(new ModSetting(28994, "CasLiara", false, 0, 0));
                Settings.Add(new ModSetting(28993, "CasAsh", false, 0, 0));
                Settings.Add(new ModSetting(28992, "CasTali", false, 0, 0));
                Settings.Add(new ModSetting(28991, "CasJav", false, 0, 0));
                Settings.Add(new ModSetting(28990, "CasKai", false, 0, 0));
                Settings.Add(new ModSetting(28989, "CasVega", false, 0, 0));
            }
            else // Is LE3
            {
                if (squadmate)
                {
                    //Outfits
                    Settings.Add(new ModSetting(28988, "CasGarrus", false, 5, 0));
                    Settings.Add(new ModSetting(28995, "CasEDI", false, 0, 0));
                    Settings.Add(new ModSetting(28994, "CasLiara", false, 0, 0));
                    Settings.Add(new ModSetting(28993, "CasAsh", false, 0, 0));
                    Settings.Add(new ModSetting(28992, "CasTali", false, 0, 0));
                    Settings.Add(new ModSetting(28991, "CasJav", false, 0, 0));
                    Settings.Add(new ModSetting(28990, "CasKai", false, 0, 0));
                    Settings.Add(new ModSetting(28989, "CasVega", false, 0, 0));
                    if (normandy)
                    {
                        //Squad
                        Settings.Add(new ModSetting(28750, "Squad", true, 0, 0));
                        Settings.Add(new ModSetting(28848, "SquadDisable", true, 0, 0));
                    }
                    if (mirandaMod)
                    {
                        //Squad
                        Settings.Add(new ModSetting(28987, "CasMirry", false, 0, 0));
                    }
                }
                if(normandy)
                {
                    //Nor
                    Settings.Add(new ModSetting(29338, "NorDocking", true, 1, 0));
                    Settings.Add(new ModSetting(28990, "NorRadio", true, 1, 0));
                    Settings.Add(new ModSetting(28857, "NorRadioGM", true, 0, 0));
                    Settings.Add(new ModSetting(28856, "NorRadioCabin", true, 1, 0));
                    Settings.Add(new ModSetting(28989, "NorMailSort", true, 1, 0));
                    //Mission Main
                    Settings.Add(new ModSetting(29337, "PrtyTuchanka", false, 0, 0));
                    Settings.Add(new ModSetting(29335, "PrtyPerseus", false, 0, 0));
                    Settings.Add(new ModSetting(29334, "PrtyThessia", false, 0, 0));
                }
                if(galMap)
                {
                    Settings.Add(new ModSetting(29338, "NorRelay", false, 0, 1));
                    Settings.Add(new ModSetting(29440, "ModAssign", false, 0, 0));
                    Settings.Add(new ModSetting(28993, "GMReapers", true, 0, 0));
                    Settings.Add(new ModSetting(28994, "GMIcons", true, 0, 0));
                    Settings.Add(new ModSetting(28992, "GMDisplayGAW", true, 0, 0));
                    Settings.Add(new ModSetting(28991, "GMDisplayFuel", true, 0, 0));
                }
                if (armorMod && shipwreckMod)
                {
                    Settings.Add(new ModSetting(28846, "ArmWreck", true, 0, 0));
                }

            }
            //Rest are core in both 
            //Mission DLC
            Settings.Add(new ModSetting(29330, "FromAshesDLC", false, 0, 0));
            Settings.Add(new ModSetting(29331, "LeviathanDLC", false, 0, 0));
            Settings.Add(new ModSetting(29332, "OmegaDLC", false, 0, 0));
            Settings.Add(new ModSetting(29333, "CitadelDLC", false, 0, 0));
            //Mission N7
            Settings.Add(new ModSetting(29430, "N7Lab", false, 0, 0));
            Settings.Add(new ModSetting(29431, "N7benning", false, 0, 0));
            Settings.Add(new ModSetting(29432, "N7tuchanka", false, 0, 0));
            Settings.Add(new ModSetting(29433, "N7ontarom", false, 0, 0));
            Settings.Add(new ModSetting(29434, "N7noveria", false, 0, 0));
            Settings.Add(new ModSetting(29435, "N7kypladon", false, 0, 0));
            //Nor
            Settings.Add(new ModSetting(28902, "NorScanner", false, 0, 0));
            Settings.Add(new ModSetting(29339, "NorArm", true, 1, 0));

        }

        private void ValidateDLC()
        {
            if(mode == MEGame.ME3)
            {
                if (ArmAPP_choice == 1 && !File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_CON_APP01\\CookedPCConsole\\Default.sfar")))
                {
                    var chkdlg = MessageBox.Show("You have set the Cerberus AAP Armor to show without having the DLC installed. This will break the armor locker. Disabled", "Warning", MessageBoxButton.OK);
                    ArmAPP_choice = 0;
                }

                bool isBroken = false;
                string missingname;
                List<string> missingrefs = new List<string>();

                if (CasEDI_choice == 2)
                {
                    missingname = "EDI in Alliance outfit";
                    if (!File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_HEN_PR\\CookedPCConsole\\Default.sfar")))
                    {
                        isBroken = true;
                        missingrefs.Add(missingname + "From Ashes DLC");
                        CasEDI_choice = 0;
                    }
                    if (!File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_MOD_EGM_Extra\\CookedPCConsole\\BioH_EDI_02_NC.pcc")))
                    {
                        isBroken = true;
                        missingrefs.Add(missingname + "EGM Custom Extras Pack");
                        CasEDI_choice = 0;
                    }
                }
                if (CasEDI_choice == 3 && (!File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_CON_APP01\\CookedPCConsole\\Default.sfar")) || !File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_MOD_EGM_Extra\\CookedPCConsole\\BioH_EDI_03_NC.pcc"))))
                {
                    var chkdlg = MessageBox.Show("EDI in Alternative armor requires the Casuals option from the EGM Custom Extras Pack and the Alternate Armor Pack DLC. Disabled.", "Warning", MessageBoxButton.OK);
                    CasEDI_choice = 0;
                }
                if (CasEDI_choice == 4 && !File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_MOD_EGM_Squad\\CookedPCConsole\\BioH_EDI_04_NC.pcc")))
                {
                    var chkdlg = MessageBox.Show("EDI in Formal outfit requires the EGM Squadmate Pack and Citadel DLC. Disabled.", "Warning", MessageBoxButton.OK);
                    CasEDI_choice = 0;
                }
                if (CasLiara_choice == 2 && (!File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_OnlinePassHidCE\\CookedPCConsole\\Default.sfar")) || !File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_MOD_EGM_Extra\\CookedPCConsole\\BioH_Liara_03_NC.pcc"))))
                {
                    var chkdlg = MessageBox.Show("Liara in Pink Broker outfit requires the Casuals option from the EGM Custom Extras Pack and the Collectors Edition DLC. Disabled.", "Warning", MessageBoxButton.OK);
                    CasLiara_choice = 0;
                }
                if (CasLiara_choice == 3 && (!File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_CON_APP01\\CookedPCConsole\\Default.sfar")) || !File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_MOD_EGM_Extra\\CookedPCConsole\\BioH_Liara_04_NC.pcc"))))
                {
                    var chkdlg = MessageBox.Show("Liara in Alternative armor requires the Casuals option from the EGM Custom Extras Pack and the Alternate Armor Pack DLC. Disabled.", "Warning", MessageBoxButton.OK);
                    CasLiara_choice = 0;
                }
                if (CasAsh_choice == 2 && (!File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_OnlinePassHidCE\\CookedPCConsole\\Default.sfar")) || !File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_MOD_EGM_Extra\\CookedPCConsole\\BioH_Ashley_02_NC.pcc"))))
                {
                    var chkdlg = MessageBox.Show("Ashley in Pink Padded outfit requires the Casuals option from the EGM Custom Extras Pack and the Collectors Edition DLC. Disabled.", "Warning", MessageBoxButton.OK);
                    CasAsh_choice = 0;
                }
                if (CasTali_choice == 2 && (!File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_HEN_PR\\CookedPCConsole\\Default.sfar")) || !File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_MOD_EGM_Extra\\CookedPCConsole\\BioH_Tali_02_NC.pcc"))))
                {
                    var chkdlg = MessageBox.Show("Tali in Faceplate requires the Casuals option from the EGM Custom Extras Pack and the From Ashes DLC. Disabled.", "Warning", MessageBoxButton.OK);
                    CasTali_choice = 0;
                }
                if (CasTali_choice == 3 && !File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_MOD_EGM_Squad\\CookedPCConsole\\BioH_Tali_03_NC.pcc")))
                {
                    var chkdlg = MessageBox.Show("Tali with hood down requires the EGM Squadmate Pack. Disabled.", "Warning", MessageBoxButton.OK);
                    CasTali_choice = 0;
                }
                if (CasTali_choice == 4 && !File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_MOD_EGM_Squad\\CookedPCConsole\\BioH_Tali_04_NC.pcc")))
                {
                    var chkdlg = MessageBox.Show("Tali in formalwear requires the EGM Squadmate Pack and the Citadel DLC. Disabled.", "Warning", MessageBoxButton.OK);
                    CasTali_choice = 0;
                }
                if (CasJav_choice == 1 && (!File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_HEN_PR\\CookedPCConsole\\Default.sfar")) || !File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_MOD_EGM_Extra\\CookedPCConsole\\BioH_Prothean_01_NC.pcc"))))
                {
                    var chkdlg = MessageBox.Show("Javik in black armor requires the Casuals extra from the EGM Custom Extra Pack and the From Ashes DLC. Disabled.", "Warning", MessageBoxButton.OK);
                    CasJav_choice = 0;
                }
                if (CasJav_choice == 2 && (!File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_HEN_PR\\CookedPCConsole\\Default.sfar")) || !File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_MOD_EGM_Extra\\CookedPCConsole\\BioH_Prothean_02_NC.pcc"))))
                {
                    var chkdlg = MessageBox.Show("Javik in black casual requires the Casuals extra from the EGM Custom Extra Pack and the From Ashes DLC. Disabled.", "Warning", MessageBoxButton.OK);
                    CasJav_choice = 0;
                }
                if (CasJav_choice == 3 && (!File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_HEN_PR\\CookedPCConsole\\Default.sfar")) || !File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_MOD_EGM_Extra\\CookedPCConsole\\BioH_Prothean_03_NC.pcc"))))
                {
                    var chkdlg = MessageBox.Show("Javik in black casual requires the Casuals extra from the EGM Custom Extra Pack and the From Ashes DLC. Disabled.", "Warning", MessageBoxButton.OK);
                    CasJav_choice = 0;
                }
                if (CasGarrus_choice == 3 && (!File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_HEN_PR\\CookedPCConsole\\Default.sfar")) || !File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_MOD_EGM_Extra\\CookedPCConsole\\BioH_Garrus_02_NC.pcc"))))
                {
                    var chkdlg = MessageBox.Show("Garrus in Camo armor requires the Casuals extra from the EGM Custom Extra Pack and the From Ashes DLC. Disabled.", "Warning", MessageBoxButton.OK);
                    CasGarrus_choice = 0;
                }
                if (CasGarrus_choice == 4 && (!File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_CON_APP01\\CookedPCConsole\\Default.sfar")) || !File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_MOD_EGM_Extra\\CookedPCConsole\\BioH_Garrus_03_NC.pcc"))))
                {
                    var chkdlg = MessageBox.Show("Garrus in Archangel Terminus armor requires the Casuals option from the EGM Custom Extras Pack and the Alternate Armor Pack DLC. Disabled.", "Warning", MessageBoxButton.OK);
                    CasGarrus_choice = 0;
                }
                if (CasGarrus_choice == 7 && !File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_MOD_EGM_Squad\\CookedPCConsole\\BioH_Garrus_06_NC.pcc")))
                {
                    var chkdlg = MessageBox.Show("Garrus in formalwear requires the EGM Squadmate Pack and the Citadel DLC. Disabled.", "Warning", MessageBoxButton.OK);
                    CasGarrus_choice = 0;
                }

                if (isBroken)
                {
                    string warnmsg = "The following required files or DLC are detected as missing:\n";
                    foreach (var s in missingrefs)
                    {
                        warnmsg = warnmsg + s + "\n";
                    }
                    if (missingrefs.Count > 1)
                    {
                        warnmsg = "\n" + "These outfits have been disabled.";
                    }
                    else
                    {
                        warnmsg = "\n" + "This outfit has been disabled.";
                    }
                    var chkdlg = MessageBox.Show(warnmsg, "Warning", MessageBoxButton.OK);

                }
            }

        }

        private void DLC_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ValidateDLC();
        }
        #endregion

        #region iniReadWrite
        private List<string> CreateIni()
        {
            var instructions = new List<string>();
            foreach (var s in Settings)
            {
                var plot = s.PlotValue.ToString();
                var type = intcmd;
                if (s.IsPlotBool)
                    type = boolcmd;
                string fieldname = $"{s.VariableLink}_choice";
                var propValue = this.GetType().GetProperty(fieldname).GetValue(this, null);
                int value = (Int32)propValue + s.OffsetValue;
                string newline = plotcmd + plot + type + value;
                instructions.Add(newline);
            }
            return instructions;
        }
        private void SaveSettings()
        {
            ValidateDLC();
            SaveSettingsVersion();
            var ini = CreateIni();

            if (File.Exists(binPath))
            {
                var attr = File.GetAttributes(binPath);
                // unset read-only
                attr = attr & ~FileAttributes.ReadOnly;
                File.SetAttributes(binPath, attr);
            }

            using (StreamWriter file = new StreamWriter(binPath))
            {
                foreach (string i in ini)
                {
                    file.WriteLine(i);
                }
            }
            StatusText = "Settings file saved";
            needsSave = false;
        }

        private void SaveSettingsVersion()
        {
            //increment save version
            UpdateVersion += 1;
            var verini = plotcmd + UpdateVersionInt + intcmd + UpdateVersion;

            if (File.Exists(verPath))
            {
                var attr = File.GetAttributes(verPath);
                // unset read-only
                attr = attr & ~FileAttributes.ReadOnly;
                File.SetAttributes(verPath, attr);
            }

            using (StreamWriter file = new StreamWriter(verPath))
            {
               file.WriteLine(verini);
            }
        }

        private void ParseSettings(string[] instructions)
        {
            foreach (var i in instructions)
            {
                if (!i.ToLower().StartsWith("init"))
                    continue;
                var valuestr = i.Substring(i.Length - 1, 1);
                bool gotVal = Int32.TryParse(valuestr, out int value);
                bool isboolcmd = false;
                if (i.Contains(boolcmd))
                    isboolcmd = true;
                var a = i.Remove(i.Length - 1, 1);
                var b = a.Replace(plotcmd, String.Empty);
                var c = b.Replace(intcmd, String.Empty);
                var d = c.Replace(boolcmd, String.Empty);
                if (Int32.TryParse(d, out int plotval) && gotVal)
                {

                    var setting = Settings.FirstOrDefault(f => f.PlotValue == plotval && f.IsPlotBool == isboolcmd);
                    if (setting != null)
                    {
                        value = value - setting.OffsetValue;
                        string fieldname = $"{setting.VariableLink}_choice";
                        this.GetType().GetProperty(fieldname)?.SetValue(this, value, null);
                    }
                }
            }
        }
        private void LoadSettingsVersion()
        {
            var version = File.ReadAllLines(verPath);
            foreach (var i in version)
            {
                if (!i.StartsWith(plotcmd + UpdateVersionInt + intcmd, StringComparison.InvariantCultureIgnoreCase))
                    continue;
                var d = i.Replace(plotcmd, String.Empty).Replace(UpdateVersionInt.ToString(), String.Empty).Replace(intcmd, String.Empty);
                if (Int32.TryParse(d, out int plotval))
                {
                   UpdateVersion = plotval;
                }
            }

        }

        private void LoadSettings()
        {
            var instructions = File.ReadAllLines(binPath);
            ParseSettings(instructions);
            StatusText = "Settings file loaded";
            needsSave = false;
        }

        private void ExportSettings()
        {
            var ini = CreateIni();
            var dlg = new SaveFileDialog();
            dlg.DefaultExt = "txt";
            dlg.Filter = "Text File|*.txt";
            dlg.Title = "Export Settings";
            dlg.ShowDialog();
            if (dlg.FileName == "")
                return;
            using (StreamWriter file = new StreamWriter(dlg.FileName))
            {
                file.WriteLine($"// EGM Settings " + mode.ToString());
                foreach (string i in ini)
                {
                    file.WriteLine(i);
                }
            }
            StatusText = "Settings file exported";
        }

        private void ImportSettings()
        {
            var dlg = new OpenFileDialog();
            dlg.DefaultExt = "txt";
            dlg.Filter = "Text File|*.txt";
            dlg.Title = "Import Settings";
            dlg.ShowDialog();
            if (dlg.FileName == "")
                return;

            var instructions = File.ReadAllLines(dlg.FileName);
            if (instructions.Length > 0 && instructions[0] == $"// EGM Settings " + mode.ToString())
            {
                ParseSettings(instructions);
                StatusText = "Settings imported";
            }
            else
            {
                StatusText = "Import failed. Unknown file.";
            }

        }
        #endregion

        #region UserCommands
        private void MoveBackTab()
        {
            currentView--;
            TabItem ti = TabCtrl.SelectedItem as TabItem;
            if(!ti.IsEnabled)
            {
                MoveBackTab();
            }
        }
        private void MoveNextTab()
        {
            currentView++;
            TabItem ti = TabCtrl.SelectedItem as TabItem;
            if (!ti.IsEnabled)
            {
                MoveNextTab();
            }
        }
        private void FinishSettings()
        {
            if (ModQP_choice != 0)
            {
                var dlg = MessageBox.Show("You have selected to play with Quick Play Mode enabled. Note this needs to be set before first boarding the Normandy, and the character's save file will be permanently  set as in this mode.", "EGM Settings", MessageBoxButton.OKCancel);
                if (dlg == MessageBoxResult.Cancel)
                    return;
            }
            if(binPath != null)
            {
                SaveSettings();
            }
            this.Close();
        }
        private void ResetToDefault()
        {
            var ddlg = MessageBox.Show("This will reset all settings to their EGM defaults. Continue?", "Reset to default", MessageBoxButton.YesNo);
            if (ddlg == MessageBoxResult.No)
                return;
            CreatingSettings();
            foreach (var set in Settings)
            {
                string fieldname = $"{set.VariableLink}_choice";
                this.GetType().GetProperty(fieldname).SetValue(this, set.DefaultValue, null);
            }
            needsSave = true;
        }
        private void ShowDiagnostics()
        {
            if (egmPath == null)
                return;
            Diagnostic = "EGM Settings " + currentBuild + "  Mode: " + mode + "  Update Count: " + UpdateVersion + "\nME3 found in " + gamePath + "\n";
            DiagnosticB = "";
            //Diagnostic = Diagnostic + "EGM version: " + egmVersion.ToString() + "\n";
            Diagnostic = Diagnostic + "From M3 install: \nSquadmate Pack: " + squadmate.ToString() + "\nGalaxy Map: " + galMap.ToString() + "\nNormandy Overhaul: " + normandy.ToString() + "\n"; 
            if(mode == MEGame.ME3)
            {
                if (!File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_MOD_EGM_MPR\\CookedPCConsole\\Default.sfar")))
                {
                    Diagnostic = Diagnostic + "\nMultiPlayer Resources (DLC_MOD_EGM_MPR) not loaded.";
                }
                else
                {
                    Diagnostic = Diagnostic + "\nMultiPlayer Resources (DLC_MOD_EGM_MPR) loaded.";
                }
                if (!File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_OnlinePassHidCE\\CookedPCConsole\\Default.sfar")))
                {
                    Diagnostic = Diagnostic + "\n\nCollectors Edition: DLC_OnlinePassHidCE not found.";
                }
                else
                {
                    Diagnostic = Diagnostic + "\n\nCollectors Edition: DLC_OnlinePassHidCE found.";
                }
                if (!File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_HEN_PR\\CookedPCConsole\\Default.sfar")))
                {
                    Diagnostic = Diagnostic + "\nFrom Ashes DLC: DLC_HEN_PR not found.";
                }
                else
                {
                    Diagnostic = Diagnostic + "\nFrom Ashes DLC: DLC_HEN_PR found.";
                }
                if (!File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_CON_APP01\\CookedPCConsole\\Default.sfar")))
                {
                    Diagnostic = Diagnostic + "\nAlternate Appearance Pack: DLC_CON_APP01 not found.";
                }
                else
                {
                    Diagnostic = Diagnostic + "\nAlternate Appearance Pack: DLC_CON_APP01 found.";
                }
                if (!File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_EXP_Pack003\\CookedPCConsole\\Default.sfar")))
                {
                    Diagnostic = Diagnostic + "\nCitadel DLC: DLC_EXP_Pack003 not found.";
                }
                else
                {
                    Diagnostic = Diagnostic + "\nCitadel DLC: DLC_EXP_Pack003 found.";
                }
                if (!File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_EXP_Pack003_Base\\CookedPCConsole\\Default.sfar")))
                {
                    Diagnostic = Diagnostic + "\nCitadel DLC Base: DLC_EXP_Pack003_Base not found.";
                }
                else
                {
                    Diagnostic = Diagnostic + "\nCitadel DLC Base: DLC_EXP_Pack003_Base found.";
                }
                if (!File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_MOD_EGM\\CookedPCConsole\\Default.sfar")))
                {
                    Diagnostic = Diagnostic + "\n\nExpanded Galaxy Mod: DLC_MOD_EGM not found.";
                }
                else
                {
                    Diagnostic = Diagnostic + "\n\nExpanded Galaxy Mod: DLC_MOD_EGM found.";
                }
                if (!File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_MOD_EGM_Squad\\CookedPCConsole\\Default.sfar")))
                {
                    Diagnostic = Diagnostic + "\nEGM Squadmate Pack: DLC_MOD_EGM_Squad not found.";
                }
                else
                {
                    Diagnostic = Diagnostic + "\nEGM Squadmate Pack: DLC_MOD_EGM_Squad found.";
                }
                if (!File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_MOD_EGM_Extra\\CookedPCConsole\\Default.sfar")))
                {
                    Diagnostic = Diagnostic + "\nEGM Custom Extras Pack: DLC_MOD_EGM_Extra not found.";
                }
                else
                {
                    Diagnostic = Diagnostic + "\nEGM Custom Extras Pack: DLC_MOD_EGM_Extra found.";
                }
                if (!File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_MOD_EGM_Ark\\CookedPCConsole\\Default.sfar")))
                {
                    Diagnostic = Diagnostic + "\nArk Mod: DLC_MOD_EGM_Ark not found.";
                }
                else
                {
                    Diagnostic = Diagnostic + "\nArk Mod: DLC_MOD_EGM_Ark found.";
                }
                if (!File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_MOD_EGM_Haz\\CookedPCConsole\\Default.sfar")))
                {
                    Diagnostic = Diagnostic + "\nArk Hazards: DLC_MOD_EGM_Haz not found.";
                }
                else
                {
                    Diagnostic = Diagnostic + "\nArk Hazards: DLC_MOD_EGM_Haz found.";
                }
                if (!File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_MOD_EGM_Miranda\\CookedPCConsole\\Default.sfar")))
                {
                    Diagnostic = Diagnostic + "\nMiranda Mod: DLC_MOD_EGM_Miranda not found.";
                }
                else
                {
                    Diagnostic = Diagnostic + "\nMiranda Mod: DLC_MOD_EGM_Miranda found.";
                }
                if (!File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_MOD_EGM_Omg\\CookedPCConsole\\Default.sfar")))
                {
                    Diagnostic = Diagnostic + "\nOmega Hub: DLC_MOD_EGM_Omg not found.";
                }
                else
                {
                    Diagnostic = Diagnostic + "\nOmega Hub: DLC_MOD_EGM_Omg found.";
                }
                if (!File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_MOD_EGM_AAP\\CookedPCConsole\\Default.sfar")))
                {
                    Diagnostic = Diagnostic + "\nAlliance Armor Pack: DLC_MOD_EGM_AAP not found.";
                }
                else
                {
                    Diagnostic = Diagnostic + "\nAlliance Armor Pack: DLC_MOD_EGM_AAP found.";
                }
                if (egmMetaData != null)
                {
                    Diagnostic = Diagnostic + "\n\nMetaData:\n" + string.Join(";\n", egmMetaData, 0, 3) + "\n";
                }
                var options = ParseMetaInstallOptions();
                if (options != null)
                {
                    foreach (var o in options)
                    {
                        Diagnostic = Diagnostic + o + "\n";
                    }
                }

                DiagnosticB = DiagnosticB + "Squadmate Pack Casuals (need Citadel DLC):";
                if (!File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_MOD_EGM_Squad\\CookedPCConsole\\BioH_Garrus_06_NC.pcc")))
                {
                    DiagnosticB = DiagnosticB + "\nGarrus Formalwear: Squad file missing.";
                }
                else
                {
                    DiagnosticB = DiagnosticB + "\nGarrus Formalwear: Squad file found.";
                }
                if (!File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_MOD_EGM_Squad\\CookedPCConsole\\BioH_EDI_04_NC.pcc")))
                {
                    DiagnosticB = DiagnosticB + "\nEDI Formalwear: Squad file missing.";
                }
                else
                {
                    DiagnosticB = DiagnosticB + "\nEDI Formalwear: Squad file found.";
                }
                if (!File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_MOD_EGM_Squad\\CookedPCConsole\\BioH_Tali_03_NC.pcc")))
                {
                    DiagnosticB = DiagnosticB + "\nTali Hood Down: Squad file missing.";
                }
                else
                {
                    DiagnosticB = DiagnosticB + "\nTali Hood Down: Squad file found.";
                }
                if (!File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_MOD_EGM_Squad\\CookedPCConsole\\BioH_Tali_04_NC.pcc")))
                {
                    DiagnosticB = DiagnosticB + "\nTali Formalwear: Squad file missing.";
                }
                else
                {
                    DiagnosticB = DiagnosticB + "\nTali Formalwear: Squad file found.";
                }

                DiagnosticB = DiagnosticB + "\n\nExtra Casuals (need Collectors Edition):";
                if (!File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_MOD_EGM_Extra\\CookedPCConsole\\BioH_Liara_03_NC.pcc")))
                {
                    DiagnosticB = DiagnosticB + "\nLiara Pink CE: Extra file missing.";
                }
                else
                {
                    DiagnosticB = DiagnosticB + "\nLiara Pink CE: Extra file found.";
                }
                if (!File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_MOD_EGM_Extra\\CookedPCConsole\\BioH_Ashley_02_NC.pcc")))
                {
                    DiagnosticB = DiagnosticB + "\nAshley CE: Extra file missing.";
                }
                else
                {
                    DiagnosticB = DiagnosticB + "\nAshley CE: Extra file found.";
                }

                DiagnosticB = DiagnosticB + "\n\nExtra Casuals (need From Ashes):";
                if (!File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_MOD_EGM_Extra\\CookedPCConsole\\BioH_Garrus_02_NC.pcc")))
                {
                    DiagnosticB = DiagnosticB + "\nGarrus Camo: Extra file missing.";
                }
                else
                {
                    DiagnosticB = DiagnosticB + "\nGarrus Camo: Extra file found.";
                }

                if (!File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_MOD_EGM_Extra\\CookedPCConsole\\BioH_EDI_02_NC.pcc")))
                {
                    DiagnosticB = DiagnosticB + "\nEDI Alliance Leather: Extra file missing.";
                }
                else
                {
                    DiagnosticB = DiagnosticB + "\nEDI Alliance Leather: Extra file found.";
                }
                if (!File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_MOD_EGM_Extra\\CookedPCConsole\\BioH_Tali_02_NC.pcc")))
                {
                    DiagnosticB = DiagnosticB + "\nTali Faceplate: Extra file missing.";
                }
                else
                {
                    DiagnosticB = DiagnosticB + "\nTali Faceplate: Extra file found.";
                }
                if (!File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_MOD_EGM_Extra\\CookedPCConsole\\BioH_Prothean_01_NC.pcc")))
                {
                    DiagnosticB = DiagnosticB + "\nJavik Black Armor: Extra file missing.";
                }
                else
                {
                    DiagnosticB = DiagnosticB + "\nJavik Black Armor: Extra file found.";
                }
                if (!File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_MOD_EGM_Extra\\CookedPCConsole\\BioH_Prothean_02_NC.pcc")))
                {
                    DiagnosticB = DiagnosticB + "\nJavik Red Casual: Extra file missing.";
                }
                else
                {
                    DiagnosticB = DiagnosticB + "\nJavik Red Casual: Extra file found.";
                }
                if (!File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_MOD_EGM_Extra\\CookedPCConsole\\BioH_Prothean_03_NC.pcc")))
                {
                    DiagnosticB = DiagnosticB + "\nJavik Black Casual: Extra file missing.";
                }
                else
                {
                    DiagnosticB = DiagnosticB + "\nJavik Black Casual: Extra file found.";
                }

                DiagnosticB = DiagnosticB + "\n\nExtra Casuals (need Alternate Appearance Pack):";
                if (!File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_MOD_EGM_Extra\\CookedPCConsole\\BioH_Garrus_03_NC.pcc")))
                {
                    DiagnosticB = DiagnosticB + "\nGarrus Terminus: Extra file missing.";
                }
                else
                {
                    DiagnosticB = DiagnosticB + "\nGarrus Terminus: Extra file found.";
                }
                if (!File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_MOD_EGM_Extra\\CookedPCConsole\\BioH_EDI_03_NC.pcc")))
                {
                    DiagnosticB = DiagnosticB + "\nEDI APP: Extra file missing.";
                }
                else
                {
                    DiagnosticB = DiagnosticB + "\nEDI APP: Extra file found.";
                }
                if (!File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_MOD_EGM_Extra\\CookedPCConsole\\BioH_Liara_04_NC.pcc")))
                {
                    DiagnosticB = DiagnosticB + "\nLiara APP: Extra file missing.";
                }
                else
                {
                    DiagnosticB = DiagnosticB + "\nLiara APP: Extra file found.";
                }

            }
            else
            {
                if (!File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_MOD_LE3Patch\\CookedPCConsole\\DLC_MOD_LE3Patch_INT.tlk")))
                {
                    Diagnostic = Diagnostic + "\nCommunity Patch: DLC_MOD_LE3Patch not found.";
                }
                else
                {
                    Diagnostic = Diagnostic + "\nCommunity Patch: DLC_MOD_LE3Patch found.";
                }
                if (!framework)
                {
                    Diagnostic = Diagnostic + "\nCommunity Framework: DLC_MOD_Framework not found.";
                }
                else
                {
                    Diagnostic = Diagnostic + "\nCommunity Framework: DLC_MOD_Framework found.";
                }
                if (!File.Exists(Path.Combine(gamePath, "BIOGame\\DLC\\DLC_MOD_EGM\\CookedPCConsole\\DLC_MOD_EGM_INT.tlk")))
                {
                    Diagnostic = Diagnostic + "\nExpanded Galaxy Mod: DLC_MOD_EGM not found.";
                }
                else
                {
                    Diagnostic = Diagnostic + "\nExpanded Galaxy Mod: DLC_MOD_EGM found.";
                }
                if (!armorMod)
                {
                    Diagnostic = Diagnostic + "\nEGM Armors for LE3: DLC_MOD_EGM_Armors not found.";
                }
                else
                {
                    Diagnostic = Diagnostic + "\nEGM Armors for LE3: DLC_MOD_EGM_Armors found.";
                }
                if (!mirandaMod)
                {
                    Diagnostic = Diagnostic + "\nMiranda Mod: DLC_MOD_EGM_Miranda not found.";
                }
                else
                {
                    Diagnostic = Diagnostic + "\nMiranda Mod: DLC_MOD_EGM_Miranda found.";
                }
                if (!shipwreckMod)
                {
                    Diagnostic = Diagnostic + "\nN7: A Spectre's Gift: DLC_MOD_EGM_Shipwreck not found.";
                }
                else
                {
                    Diagnostic = Diagnostic + "\nN7: A Spectre's Gift DLC_MOD_EGM_Shipwreck found.";
                }

                if (egmMetaData != null)
                {
                    DiagnosticB = DiagnosticB + "\n\nMetaData:\n" + string.Join(";\n", egmMetaData, 0, 3) + "\n";
                }
                var options = ParseMetaInstallOptions();
                if (options != null)
                {
                    foreach (var o in options)
                    {
                        DiagnosticB = DiagnosticB + o + "\n";
                    }
                }
            }

            DiagnosticB = DiagnosticB + "\n\nCopied to clipboard...\n";

            string textSetting = "";
            foreach (var s in Settings)
            {
                string fieldname = $"{s.VariableLink}_choice";
                var propValue = this.GetType().GetProperty(fieldname).GetValue(this, null);
                int value = (Int32)propValue + s.OffsetValue;
                textSetting = textSetting + value.ToString();
            }
            Clipboard.SetText(Diagnostic + DiagnosticB + $"{textSetting}\n" + textSetting);
            tab_Diagnostics.Visibility = Visibility.Visible;
            currentView = 7;
        }

        private void arkvsr_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ArkVsr_choice == 0)
                return;
            var dlg = MessageBox.Show($"EGM Settings will now switch visor files to {ArkVsr_cln[ArkVsr_choice]} and Autotoc.", "Visor Selection", MessageBoxButton.OKCancel);

            if(dlg == MessageBoxResult.Cancel)
            {
                ArkVsr_choice = 0;
                return;
            }

            switch(ArkVsr_choice)
            {
                case 1:
                    File.Copy(Path.Combine(me3Path, "BIOGame\\DLC\\DLC_MOD_EGM_Ark\\CookedPCConsole\\BIOG_ARK_HGR_BRO_R_TRANS.pcc"), Path.Combine(me3Path, "BIOGame\\DLC\\DLC_MOD_EGM_Ark\\CookedPCConsole\\BIOG_ARK_HGR_BRO_R.pcc"), true);
                    File.Copy(Path.Combine(me3Path, "BIOGame\\DLC\\DLC_MOD_EGM_Ark\\CookedPCConsole\\BIOG_ARK_HGR_FEM_R_TRANS.pcc"), Path.Combine(me3Path, "BIOGame\\DLC\\DLC_MOD_EGM_Ark\\CookedPCConsole\\BIOG_ARK_HGR_FEM_R.pcc"), true);
                    break;
                case 2:
                    File.Copy(Path.Combine(me3Path, "BIOGame\\DLC\\DLC_MOD_EGM_Ark\\CookedPCConsole\\BIOG_ARK_HGR_BRO_R_OPAQ.pcc"), Path.Combine(me3Path, "BIOGame\\DLC\\DLC_MOD_EGM_Ark\\CookedPCConsole\\BIOG_ARK_HGR_BRO_R.pcc"), true);
                    File.Copy(Path.Combine(me3Path, "BIOGame\\DLC\\DLC_MOD_EGM_Ark\\CookedPCConsole\\BIOG_ARK_HGR_FEM_R_OPAQ.pcc"), Path.Combine(me3Path, "BIOGame\\DLC\\DLC_MOD_EGM_Ark\\CookedPCConsole\\BIOG_ARK_HGR_FEM_R.pcc"), true);
                    break;
                default:
                    break;
            }
            GenerateTOCS();
        }

        public async void GenerateTOCS()
        {
            StatusText = "Please wait Generating TOCs...";
            TabCtrl.IsEnabled = false;
            Finish_btn.IsEnabled = false;
            await Task.Run(() => AutoTOC.Program.RunAutoTOC(gamePath));
            TabCtrl.IsEnabled = true;
            Finish_btn.IsEnabled = true;
            StatusText = "TOCs generated.";
        }
        #endregion

        #region HelpPanels
        private void Mod_MouseOver(object sender, MouseEventArgs e)
        {
            var source = e.OriginalSource as FrameworkElement;
            var tag = source.Tag.ToString();
            if (tag == displayedHelp)
                return;
            displayedHelp = tag;
            switch (tag)
            {
                case "ModBeta":
                    mod_help_title.Text = ModWARBeta_TITLE;
                    mod_help_text.Text = ModWARBeta_TXT + ModWARBeta2_TXT + ModWARBeta3_TXT;
                    break;
                case "ModQP":
                    mod_help_title.Text = ModQP_TITLE;
                    mod_help_text.Text = ModQP_TXT;
                    break;
                case "ModAss":
                    if(mode == MEGame.ME3)
                    {
                        mod_help_title.Text = ModAssign_TITLE;
                        mod_help_text.Text = ModAssign_TXT;
                    }
                    else
                    {
                        mod_help_title.Text = ModAssign_TITLE;
                        mod_help_text.Text = ModAssignLE_TXT;
                    }
                    break;
                case "ModEggs":
                    mod_help_title.Text = ModEggs_TITLE;
                    mod_help_text.Text = ModEggs_TXT;
                    break;
                case "ModALOT":
                    mod_help_title.Text = ModALOT_TITLE;
                    mod_help_text.Text = ModALOT_TXT;
                    break;
                case "ModReset":
                    mod_help_title.Text = ModReset_TITLE;
                    mod_help_text.Text = ModReset_TXT;
                    break;
                case "ModExport":
                    mod_help_title.Text = ModExport_TITLE;
                    mod_help_text.Text = ModExport_TXT;
                    break;
                case "ModImport":
                    mod_help_title.Text = ModImport_TITLE;
                    mod_help_text.Text = ModImport_TXT;
                    break;
                default:
                    mod_help_title.Text = Mod_Help_TITLE;
                    mod_help_text.Text = Mod_Help_TXT;
                    break;
            }
        }
        private void Normandy_MouseOver(object sender, MouseEventArgs e)
        {
            var source = e.OriginalSource as FrameworkElement;
            var tag = source.Tag.ToString();
            if (tag == displayedHelp)
                return;
            displayedHelp = tag;
            switch (tag)
            {
                case "NorDock":
                    nor_help_title.Text = NorDocking_TITLE;
                    nor_help_text.Text = NorDocking_TXT;
                    break;
                case "NorRelay":
                    nor_help_title.Text = NorRelay_TITLE;
                    nor_help_text.Text = NorRelay_TXT;
                    break;
                case "NorArm":
                    nor_help_title.Text = NorArm_TITLE;
                    nor_help_text.Text = NorArm_TXT;
                    break;
                case "NorScan":
                    nor_help_title.Text = NorScanner_TITLE;
                    nor_help_text.Text = NorScanner_TXT;
                    break;
                case "NorStereo":
                    nor_help_title.Text = NorStereo_TITLE;
                    nor_help_text.Text = NorStereo_TXT;
                    break;
                case "NorCabMus":
                    nor_help_title.Text = NorCabinMus_TITLE;
                    nor_help_text.Text = NorCabinMus_TXT;
                    break;
                case "NorRadio":
                    nor_help_title.Text = NorRadio_TITLE;
                    nor_help_text.Text = NorRadio_TXT;
                    break;
                case "NorRadioGM":
                    nor_help_title.Text = NorRadioGM_TITLE;
                    nor_help_text.Text = NorRadioGM_TXT;
                    break;
                case "NorRadioCabin":
                    nor_help_title.Text = NorRadioCabin_TITLE;
                    nor_help_text.Text = NorRadioCabin_TXT;
                    break;
                case "GMReapers":
                    nor_help_title.Text = GMReapers_TITLE;
                    nor_help_text.Text = GMReapers_TXT;
                    break;
                case "GMIcons":
                    nor_help_title.Text = GMIcons_TITLE;
                    nor_help_text.Text = GMIcons_TXT;
                    break;
                case "GMDisplayGAW":
                    nor_help_title.Text = GMDisplayGAW_TITLE;
                    nor_help_text.Text = GMDisplayGAW_TXT;
                    break;
                case "GMDisplayFuel":
                    nor_help_title.Text = GMDisplayFuel_TITLE;
                    nor_help_text.Text = GMDisplayFuel_TXT;
                    break;
                case "NorMail":
                    nor_help_title.Text = NorMailSort_TITLE;
                    nor_help_text.Text = NorMailSort_TXT;
                    break;
                default:
                    nor_help_title.Text = "Normandy Settings";
                    nor_help_text.Text = "Various settings on the Normandy\n\n"+ Mod_Help_TXT;
                    break;
            }
        }
        private void Missions_MouseOver(object sender, MouseEventArgs e)
        {
            var source = e.OriginalSource as FrameworkElement;
            var tag = source.Tag.ToString();
            if (tag == displayedHelp)
                return;
            displayedHelp = tag;
            switch (tag)
            {
                case "HEN_PR":
                    missions_help_title.Text = FromAshesDLC_TITLE;
                    missions_help_text.Text = FromAshesDLC_TXT;
                    break;
                case "EXP001":
                    missions_help_title.Text = LeviathanDLC_TITLE;
                    missions_help_text.Text = LeviathanDLC_TXT;
                    break;
                case "EXP002":
                    missions_help_title.Text = OmegaDLC_TITLE;
                    missions_help_text.Text = OmegaDLC_TXT;
                    break;
                case "EXP003":
                    missions_help_title.Text = CitadelDLC_TITLE;
                    missions_help_text.Text = CitadelDLC_TXT;
                    break;
                case "PRTUCHANKA":
                    missions_help_title.Text = PrtyTuchanka_TITLE;
                    missions_help_text.Text = PrtyTuchanka_TXT;
                    break;
                case "PRPV":
                    missions_help_title.Text = PrtyPerseus_TITLE;
                    missions_help_text.Text = PrtyPerseus_TXT;
                    break;
                case "PRC3":
                    missions_help_title.Text = PrtyThessia_TITLE;
                    missions_help_text.Text = PrtyThessia_TXT;
                    break;
                case "N7LAB":
                    missions_help_title.Text = N7Lab_TITLE;
                    missions_help_text.Text = N7Lab_TXT;
                    break;
                case "N7BEN":
                    missions_help_title.Text = N7benning_TITLE;
                    missions_help_text.Text = N7benning_TXT;
                    break;
                case "N7ATK":
                    missions_help_title.Text = N7tuchanka_TITLE;
                    missions_help_text.Text = N7tuchanka_TXT;
                    break;
                case "N7ONT":
                    missions_help_title.Text = N7ontarom_TITLE;
                    missions_help_text.Text = N7ontarom_TXT;
                    break;
                case "N7NOV":
                    missions_help_title.Text = N7noveria_TITLE;
                    missions_help_text.Text = N7noveria_TXT;
                    break;
                case "N7CYO":
                    missions_help_title.Text = N7kypladon_TITLE;
                    missions_help_text.Text = N7kypladon_TXT;
                    break;
                default:
                    missions_help_title.Text = Mission_TITLE;
                    missions_help_text.Text = Mission_TXT;
                    break;
            }
        }
        private void Outfit_MouseOver(object sender, MouseEventArgs e)
        {
            var source = e.OriginalSource as FrameworkElement;
            var tag = source.Tag.ToString();
            if (tag == displayedHelp)
                return;
            displayedHelp = tag;
            img_garrus.Visibility = Visibility.Collapsed;
            img_edi.Visibility = Visibility.Collapsed;
            img_liara.Visibility = Visibility.Collapsed;
            img_ash.Visibility = Visibility.Collapsed;
            img_tali.Visibility = Visibility.Collapsed;
            img_kaidan.Visibility = Visibility.Collapsed;
            img_javik.Visibility = Visibility.Collapsed;
            img_vega.Visibility = Visibility.Collapsed;
            img_mirandaLE.Visibility = Visibility.Collapsed;
            switch (tag)
            {
                case "ArmAll":
                    outfits_help_title.Text = N7armAlliance_TITLE;
                    outfits_help_text.Text = N7armAlliance_TXT;
                    break;
                case "ArmAPP":
                    outfits_help_title.Text = N7ArmAPP_TITLE;
                    outfits_help_text.Text = N7ArmAPP_TXT;
                    break;
                case "CasGar":
                    outfits_help_title.Text = N7casgarrus_TITLE;
                    outfits_help_text.Text = N7casgarrus_TXT;
                    img_garrus.Visibility = Visibility.Visible;
                    break;
                case "CasEDI":
                    outfits_help_title.Text = N7casedi_TITLE;
                    outfits_help_text.Text = N7casedi_TXT;
                    img_edi.Visibility = Visibility.Visible;
                    break;
                case "CasLiara":
                    outfits_help_title.Text = N7casliara_TITLE;
                    outfits_help_text.Text = N7casliara_TXT;
                    img_liara.Visibility = Visibility.Visible;
                    break;
                case "CasAsh":
                    outfits_help_title.Text = N7casash_TITLE;
                    outfits_help_text.Text = N7casash_TXT;
                    img_ash.Visibility = Visibility.Visible;
                    break;
                case "CasTali":
                    outfits_help_title.Text = N7castali_TITLE;
                    outfits_help_text.Text = N7castali_TXT;
                    img_tali.Visibility = Visibility.Visible;
                    break;
                case "CasKai":
                    outfits_help_title.Text = N7caskai_TITLE;
                    outfits_help_text.Text = N7caskai_TXT;
                    img_kaidan.Visibility = Visibility.Visible;
                    break;
                case "CasJav":
                    outfits_help_title.Text = N7casjav_TITLE;
                    outfits_help_text.Text = N7casjav_TXT;
                    img_javik.Visibility = Visibility.Visible;
                    break;
                case "CasVega":
                    outfits_help_title.Text = N7casvega_TITLE;
                    outfits_help_text.Text = N7casvega_TXT;
                    img_vega.Visibility = Visibility.Visible;
                    break;
                case "CasMirry":
                    outfits_help_title.Text = N7casmirry_TITLE;
                    outfits_help_text.Text = N7casmirry_TXT;
                    img_mirandaLE.Visibility = Visibility.Visible;
                    break;
                default:
                    outfits_help_title.Text = "Squadmate Casual Attire";
                    outfits_help_text.Text = "You can switch attire whenever you want, just save and reload whilst on the Normandy. The outfits will be worn on the Normandy, Citadel and parts of the Citadel DLC.\n\nSome outfits require DLC and EGM custom extras installed.";
                    break;
            }
        }
        private void Misc_MouseOver(object sender, MouseEventArgs e)
        {
            var source = e.OriginalSource as FrameworkElement;
            var tag = source.Tag.ToString();
            if (tag == displayedHelp)
                return;
            displayedHelp = tag;
            img_miranda.Visibility = Visibility.Collapsed;
            img_arkhgr.Visibility = Visibility.Collapsed;
            switch (tag)
            {
                case "CasMir":
                    misc_help_title.Text = N7casmiranda_TITLE;
                    misc_help_text.Text = N7casmiranda_TXT;
                    img_miranda.Visibility = Visibility.Visible;
                    break;
                case "ArkPal":
                    misc_help_title.Text = ArkN7Paladin_TITLE;
                    misc_help_text.Text = ArkN7Paladin_TXT;
                    break;
                case "ArkBen":
                    misc_help_title.Text = ArkBenning_TITLE;
                    misc_help_text.Text = ArkBenning_TXT;
                    break;
                case "ArkVsr":
                    misc_help_title.Text = ArkVsr_TITLE;
                    misc_help_text.Text = ArkVsr_TXT;
                    img_arkhgr.Visibility = Visibility.Visible;
                    break;
                case "MiscDiag":
                    misc_help_title.Text = "Diagnostics";
                    misc_help_text.Text = "Show Diagnostics Panel of which Mass Effect 3 and EGM modules are installed.";
                    break;
                case "ArmWreck":
                    misc_help_title.Text = ArmWreck_TITLE;
                    misc_help_text.Text = ArmWreck_TXT;
                    break;
                default:
                    misc_help_title.Text = "";
                    misc_help_text.Text = "";
                    break;
            }
        }
        private void Squad_MouseOver(object sender, MouseEventArgs e)
        {
            BonusSquad_txt.Visibility = Visibility.Collapsed;
            BonusSquad_txt_LE.Visibility = Visibility.Collapsed;
            storyMode_txt.Visibility = Visibility.Visible;
        }
        private void Squad_MouseLeft(object sender, MouseEventArgs e)
        {
            storyMode_txt.Visibility = Visibility.Collapsed;
            if (mode == MEGame.LE3)
            {
                BonusSquad_txt_LE.Visibility = Visibility.Visible;
            }
            else
            {
                BonusSquad_txt.Visibility = Visibility.Visible;
            }
        }

        #endregion

    }

    public class ModSetting
    {
        public int PlotValue;

        public string VariableLink;

        public bool IsPlotBool;

        public int DefaultValue;

        public int OffsetValue;

        public ModSetting(int plotvalue, string variablelink, bool isplotbool, int defaultvalue, int offsetvalue)
        {
            PlotValue = plotvalue;
            VariableLink = variablelink;
            IsPlotBool = isplotbool;
            DefaultValue = defaultvalue;
            OffsetValue = offsetvalue;
        }

    }

    public class GenericCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public GenericCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }
        public bool CanExecute(object parameter)
        {
            bool result = _canExecute?.Invoke() ?? true;
            return result;
        }

        public void Execute(object parameter)
        {
            _execute();
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }

}
