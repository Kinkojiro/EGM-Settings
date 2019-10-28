using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace EGMSettings
{
    /// <summary>
    /// Interaction logic for EGMSettings.xaml
    /// </summary>
    public partial class SettingsPanel : NotifyPropertyChangedWindowBase
    {
        #region SystemVars
        private int _currentView;
        public int currentView { get => _currentView; set => SetProperty(ref _currentView, value); }
        private string displayedHelp;
        private string binPath;
        private string me3Path;
        private List<ModSetting> Settings = new List<ModSetting>();
        private const string plotcmd = "InitPlotManagerValueByIndex ";
        private const string boolcmd = " bool ";
        private const string intcmd = " int ";
        private string _statusText = "v1.2";
        public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }
        private bool needsSave;
        public ICommand SaveCommand { get; set; }
        public ICommand NextCommand { get; set; }
        public ICommand BackCommand { get; set; }
        public ICommand FinishCommand { get; set; }
        private bool CanNextCommand()
        {
            return currentView < 5;
        }
        private bool CanBackCommand()
        {
            return currentView > 0;
        }
        #endregion

        #region ModVars
        public const string Mod_Help_TITLE = "Various Mod Settings";
        public const string Mod_Help_TXT = "Hover the mouse over the settings to see more information.";
        private int _modWARBeta_choice = 0; //NEEDS +1 ADDED
        public int ModWARBeta_choice { get => _modWARBeta_choice; set { SetProperty(ref _modWARBeta_choice, value); needsSave = true; } }
        private ObservableCollection<string> _modWARBeta_cln = new ObservableCollection<string>() { "Easy: Default ME3 setting (6200 / 6200)", "Galactic war: (6800 / 6250)", "Extinction event: (7100 / 6500)" };
        public ObservableCollection<string> ModWARBeta_cln { get => _modWARBeta_cln; }
        private const string ModWARBeta_TITLE = "War Asset Beta - Difficulty Settings";
        private const string ModWARBeta_TXT = "THIS IS ONLY APPLIED IF YOU HAVE THE BETA INSTALLED.\n\nGalactic Readiness is permanently  set to 100% in the Beta (no need for multiplayer). Target score depends on difficulty and whether you are playing with an ME2 import. \n\nIMPORTANT: once Priority: Surkesh is finished this setting cannot be changed. The first time you load the Normandy up after this mission, it will be fixed.\n\n";
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
        private const string ModQP_TXT = "This mode is designed for players who want a quick playthrough focused on combat and story, and without RPG elements such as the War Asset System.\n\nThis mode:\n- disables the war asset system so no need to recover war assets or do side missions.\n- gives the player an offsetting bonus that means they will always get the highest war asset outcome\n- hides every cluster in the galaxy map that doesn't have a playable combat mission in it at some point.\n- disables the War Asset Terminals.\n\nNOTE THIS MUST BE SET BEFORE THE END OF THE PROLOGUE (CITADEL I)\nONCE SET IT CANNOT BE UNSET (EVEN IF EGM IS UNINSTALLED YOUR SAVE WILL AUTOMATICALLY BE SET TO MAXIMUM).";

        private int _modAssign_choice = 0;
        public int ModAssign_choice { get => _modAssign_choice; set { SetProperty(ref _modAssign_choice, value); needsSave = true; } }
        private ObservableCollection<string> _modAssign_cln = new ObservableCollection<string>() { "EGM Assignments on (default)", "EGM Assignments off" };
        public ObservableCollection<string> ModAssign_cln { get => _modAssign_cln; }
        private const string ModAssign_TITLE = "EGM Assignments";
        private const string ModAssign_TXT = "EGM has a 15 assignments.  These are short (often text based) fetch quests, similar to the ones in the default game but with an added twist. They give extra war assets, credits, choices and paragon/renegade bonuses.\n\nThe assignments include the Evacuation of Thessia minigame and the quest that leads to the Prothean Cybernetics.\n\nIf you don't want the added assignments switch this off.\n\nNote once the assignment has been given, it will remain active and can be completed.  If you switch off after this setting after you have completed the assignment you will keep the rewards.";

        #endregion

        #region NormandyVars
        private int _norScanner_choice = 0;
        public int NorScanner_choice { get => _norScanner_choice; set { SetProperty(ref _norScanner_choice, value); needsSave = true; } }
        private ObservableCollection<string> _norScanner_cln = new ObservableCollection<string>() { "Walk through (about 0.75secs)", "Sprint through (no pause)", "Full scan (about 4 secs)" };
        public ObservableCollection<string> NorScanner_cln { get => _norScanner_cln; }
        private const string NorScanner_TITLE = "Normandy Security Scanner";
        private const string NorScanner_TXT = "Select how fast the security scanner scans Shepard.\n\nEGM default: Shepard can walk through, ME3 default: Player has to stop for 4 seconds, or select so that the scanner can be ignored.\n\nNo graphical glitches.";
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
        private const string NorRelay_TXT = "Select which Relay video to use when transiting clusters on the galaxy map.\n\nNote if you select the No video option there maybe slight graphical glitches that the transition is designed to hide.";
        private int _norArm_choice = 1;
        public int NorArm_choice { get => _norArm_choice; set { SetProperty(ref _norArm_choice, value); needsSave = true; } }
        private ObservableCollection<string> _norArm_cln = new ObservableCollection<string>() { "Weapons & Squad selection only", "Armor, Weapons & Squad selection" };
        public ObservableCollection<string> NorArm_cln { get => _norArm_cln; }
        private const string NorArm_TITLE = "Armor Selection on Mission Launch";
        private const string NorArm_TXT = "When launching a combat mission from the Normandy choose armor as well as weapons and squadmates.";
        private int _norRadio_choice = 1;
        public int NorRadio_choice { get => _norRadio_choice; set { SetProperty(ref _norRadio_choice, value); needsSave = true; } }
        private ObservableCollection<string> _norRadio_cln = new ObservableCollection<string>() { "Stereo disabled", "Normandy Stereo enabled" };
        public ObservableCollection<string> NorRadio_cln { get => _norRadio_cln; }
        private const string NorRadio_TITLE = "Normandy Stereo";
        private const string NorRadio_TXT = "Enable or disable the stereo.  There are switches on each deck.\n\nMore tunes are available as you find them in the Citadel DLC.";
        private int _norCabinMus_choice = 1;
        public int NorCabinMus_choice { get => _norCabinMus_choice; set { SetProperty(ref _norCabinMus_choice, value); needsSave = true; } }
        private ObservableCollection<string> _norCabinMus_cln = new ObservableCollection<string>() { "Cabin Music Player", "Normandy Stereo" };
        public ObservableCollection<string> NorCabinMus_cln { get => _norCabinMus_cln; }
        private const string NorCabinMus_TITLE = "Music during Cabin Invites";
        private const string NorCabinMus_TXT = "When your love interest is invited up to the cabin the stereo will automatically start.  It will automatically stop when you exit the cabin.\n\nConfirm which music player to use: the Normandy stereo or the default Cabin Music Player.\n\nNote: If you have the Better Cabin Music Mod then switch to the Cabin player to hear that mod's music instead.";
        #endregion

        #region MissionVars
        public const string Mission_TITLE = "Mission Timings";
        public const string Mission_TXT = "Use these options to customise when missions become available. If you want to wait to do a DLC or mission, using these options will prevent the mission being given until you decide.\n\nIf you set it to an already completed period it will usually fire immediately. Once you have received a mission, these settings have no effect.\n\nThese settings are designed so entire DLC mission arcs can be completed without having Shepard being told they must be somewhere immediately.";
        private int _FromAshesDLC_choice = 0;
        public int FromAshesDLC_choice { get => _FromAshesDLC_choice; set { SetProperty(ref _FromAshesDLC_choice, value); needsSave = true; } }
        private ObservableCollection<string> _fromAshesDLC_cln = new ObservableCollection<string>() { "Post Mars (default)", "Post Palaven", "Post Surkesh", "Post Tuchanka", "Post Coup" };
        public ObservableCollection<string> FromAshesDLC_cln { get => _fromAshesDLC_cln; }
        private const string FromAshesDLC_TITLE = "From Ashes DLC";
        private const string FromAshesDLC_TXT = "Set this to determine when you get an email from Admiral Hackett about a potential opportunity.\n\nUnlocks mission Priority: Eden Prime";
        private int _LeviathanDLC_choice = 0;
        public int LeviathanDLC_choice { get => _LeviathanDLC_choice; set { SetProperty(ref _LeviathanDLC_choice, value); needsSave = true; } }
        private ObservableCollection<string> leviathanDLC_cln = new ObservableCollection<string>() { "Post Palaven (default)", "Post Surkesh", "Post Tuchanka", "Post Geth Dreadnought", "Post Thessia" };
        public ObservableCollection<string> LeviathanDLC_cln { get => leviathanDLC_cln; }
        private const string LeviathanDLC_TITLE = "Leviathan DLC";
        private const string LeviathanDLC_TXT = "Set this to determine when you get an email Admiral Hackett about a scientist on the Citadel.\n \nUnlocks Dr Bryson's Laboratory on the Citadel.";
        private int _OmegaDLC_choice = 0;
        public int OmegaDLC_choice { get => _OmegaDLC_choice; set { SetProperty(ref _OmegaDLC_choice, value); needsSave = true; } }
        private ObservableCollection<string> _omegaDLC_cln = new ObservableCollection<string>() { "Post Palaven (default)", "Post Surkesh", "Post Citadel Coup", "Post Geth Dreadnought", "Post Thessia" };
        public ObservableCollection<string> OmegaDLC_cln { get => _omegaDLC_cln; }
        private const string OmegaDLC_TITLE = "Omega DLC";
        private const string OmegaDLC_TXT = "Set this to determine when you get an email from Aria inviting you to meet her on the Citadel at Dock 42.\n\nIn addition you must have met with her in the Purgatory nightclub on the Citadel at least once.\n\nUnlocks Dock 42 on the Citadel.";
        private int _CitadelDLC_choice = 0;
        public int CitadelDLC_choice { get => _CitadelDLC_choice; set { SetProperty(ref _CitadelDLC_choice, value); needsSave = true; } }
        private ObservableCollection<string> _citadelDLC_cln = new ObservableCollection<string>() { "Post Coup (default)", "Post Coup + 2 missions", "Post Rannoch", "Post Thessia", "Post Horizon" };
        public ObservableCollection<string> CitadelDLC_cln { get => _citadelDLC_cln; }
        private const string CitadelDLC_TITLE = "Citadel DLC";
        private const string CitadelDLC_TXT = "Set this to determine when you get an email Admiral Hackett about shore leave.\n\nUnlocks Citadel: Shore Leave.";
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
        private const string PrtyPerseus_TXT = "This runs a video conference with Hackett - Post Coup discussion on Quarians.\n\nBy default this is given as part of the post coup debrief.\n\nIf you choose the alternative option a button in the video conference room will appear. You can use it to connect to Hackett whenever you want.\n\nIMPORTANT: This option is only available for English speaking versions of the game.\n\nUnlocks Priority: Perseus Veil";
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
        private const string N7Lab_TXT = "Set when Traynor tells you about this N7 mission on Sanctum.\n\nNOTE: to complete Citadel: Alien Medi-Gel Formula if setting post-Coup or post Thessia the formula will be available from Spectre Requisitions. You must turn it in before completing Priority: Tuchanka.\n\nUnlocks N7: Cerberus Labs";
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
        private const string N7kypladon_TXT = "Set when Traynor tells you about this N7 mission on Cyone.\n\nNOTE: EGM has a slightly different default setting to the vanilla. Post Geth-Dreadnought you are presented with several very urgent missions, so this mission becomes a distraction. With the EGM default it becomes available after completing 3 of:\nPriority: Geth Dreadnought\nRannoch: Admiral\nGeth Fighter Squadrons\nPriority: Rannoch.\n\nUnlocks N7: Fuel Reactors";

        #endregion

        #region SquadVars
        private int _squad_choice = 0;
        public int Squad_choice { get => _squad_choice; set { SetProperty(ref _squad_choice, value); needsSave = true; } }
        private ObservableCollection<string> _squad_cln = new ObservableCollection<string>() { "Story Mode (default)", "Non-Story Mode (all available)" };
        public ObservableCollection<string> Squad_cln { get => _squad_cln; }
        public const string Squad_StoryMode = "WREX:  Between Priority: Surkesh and Priority: Tuchanka   (will not be available for Rescue/Bomb missions)\nJACK:  For up to 2 missions past Grissom Academy (assuming you don't drop her/students off at the Citadel) then post meeting in Purgatory.\nMIRANDA:  Post Horizon  (requires Miranda Mod add-on)\nJACOB:  After saving on Gellix and speaking in Huerta Memorial Hospital\nSAMARA:  After saving in Mesana and speaking in the Citadel Embassy.\nGRUNT:  After completing the Rachni mission and getting rid of C-SEC on the Citadel.\nKASUMI: Post recruiting during Hanar Diplomat.\nZAEED:  Post recruiting during Volus Ambassador plot (need to speak afterwards).\nARIA:  REQUIRES OMEGA DLC.  Once Omega is completed and only if Shepard proved their complete loyalty.";
        public const string Squad_Notes = "- All bonus squadmates are only available if the Citadel DLC is installed. Aria requires the Omega DLC.\n- Only certain maps have been unlocked for use with the extra squadmates.\n- Default unlocked maps: N7: Labs (Sanctum), N7: Tuchanka, N7: Ontarom, N7: Benning, N7: Noveria, N7: Cyone\n- Additional maps can be found in a seperate add-on pack available on Nexus.\n- Even if a squadmate is set to be available they will appear greyed out and unselectable in the GUI if you try to take them to a mission for which the map is not unlocked.\n- If you take one regular squadmate and one new, you will get all the usual squad chatter (the regular squadmate will speak extra lines).";
        #endregion

        #region MiscVars
        private int _armAlliance_choice = 1;
        public int ArmAlliance_choice { get => _armAlliance_choice; set { SetProperty(ref _armAlliance_choice, value); needsSave = true; } }
        private ObservableCollection<string> _armAlliance_cln = new ObservableCollection<string>() { "Hide all", "Show all (default)" };
        public ObservableCollection<string> ArmAlliance_cln { get => _armAlliance_cln; }
        private const string N7armAlliance_TITLE = "Alliance Standard Armors";
        private const string N7armAlliance_TXT = "Show extra Alliance standard armors in the armor locker:\n\nFull body: Special Forces Heavy, Special Forces Medium\nTorso: Marine Officer (tintable), Phoenix (Femshep), Marine (Maleshep)\nHelmet: Standard issue breather";
        private int _armAAP_choice = 0;
        public int ArmAAP_choice { get => _armAAP_choice; set { SetProperty(ref _armAAP_choice, value); needsSave = true; } }
        private ObservableCollection<string> _armAAP_cln = new ObservableCollection<string>() { "Is not installed (default)", "Alternative Appearance Pack is installed" };
        public ObservableCollection<string> ArmAAP_cln { get => _armAAP_cln; }
        private const string N7armAAP_TITLE = "Cerberus Ajax Armor";
        private const string N7armAAP_TXT = "Unlocks the torso and helmet as seperate items, and makes the armor appear.\n\nWARNING: IF YOU UNLOCK THIS WITHOUT HAVING BIOWARE'S ALTERNATIVE APPEARANCE PACK DLC IT WILL BREAK THE ARMOR LOCKER.";
        private int _casGarrus_choice = 1;
        public int CasGarrus_choice { get => _casGarrus_choice; set { SetProperty(ref _casGarrus_choice, value); needsSave = true; } }
        private ObservableCollection<string> _casGarrus_cln = new ObservableCollection<string>() { "Armored Garrus (ME3 default)", "Casual Garrus (EGM default)" };
        public ObservableCollection<string> CasGarrus_cln { get => _casGarrus_cln; }
        private const string N7casgarrus_TITLE = "Casual Garrus";
        private const string N7casgarrus_TXT = "When on the Normandy and Citadel Presidium Garrus will wear his casual outfit. Between the Citadel DLC missions he will wear armor, but for the party can be casual. He will always wear casual if invited to the cabin when romanced.\n\nNOTE: Don't change this when Garrus is in the room with Shepard. Leave the area (or deck) then save and reload. Otherwise he can disappear.";
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
        #endregion

        #region Initial/Exit
        public SettingsPanel()
        {
            LoadCommands();
            InitializeComponent();
            CreatingSettings();

            var binDir = GetBinDirectory();
            binPath = Path.Combine(binDir, "EGMSettings.ini");
        }

        private void LoadCommands()
        {
            SaveCommand = new GenericCommand(SaveSettings);
            NextCommand = new GenericCommand(MoveNextTab, CanNextCommand);
            BackCommand = new GenericCommand(MoveBackTab, CanBackCommand);
            FinishCommand = new GenericCommand(FinishSettings);
        }

        private void EGMSettings_Loaded(object sender, RoutedEventArgs e)
        {
            //Load ini or create
            if(binPath == null || me3Path == null)
            {
                StatusText = "Mass Effect 3 install not found. Changes will not be saved";
                status_TxtBx.Foreground = Brushes.Red;
                status_TxtBx.FontWeight = FontWeights.Bold;
                return;
            }
            
            if(!File.Exists(binPath))
            {
                //First time set AAP to correct value
                var aapDir = Directory.GetDirectories(me3Path, "DLC_CON_APP01", SearchOption.AllDirectories).Any();
                if(aapDir)
                {
                    ArmAAP_choice = 1;
                }
                SaveSettings();
                StatusText = "Default EGM settings file created.";
            }
            else
            {
                LoadSettings();
                StatusText = "Existing EGM settings file loaded.";
            }
        }

        private void EGMSettings_Closing(object sender, CancelEventArgs e)
        {
            //Check if ini data needs saving
            if(needsSave && binPath != null)
            {
                var xdlg = MessageBox.Show("Changes have not been saved. Save now?", "EGM Settings", MessageBoxButton.YesNo);
                if (xdlg == MessageBoxResult.Yes)
                {
                    SaveSettings();
                }
            }
        }
        
        private string GetBinDirectory()
        {
            string basePath = null;
            string appPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            if(appPath != null)
            {
                basePath = FindME3ParentDir(appPath);
                if(basePath == null)
                {
                    //get ME3Directory from registry
                    string hkey32 = @"HKEY_LOCAL_MACHINE\SOFTWARE\";
                    string hkey64 = @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\";
                    string subkey = @"BioWare\Mass Effect 3";

                    string keyName = hkey32 + subkey;
                    basePath = (string)Microsoft.Win32.Registry.GetValue(keyName, "Install Dir", null);
                    if (basePath == null)
                    {
                        keyName = hkey64 + subkey;
                        basePath = (string)Microsoft.Win32.Registry.GetValue(keyName, "Install Dir", null);
                    }
                }
            }

            if(basePath != null)
            {
                me3Path = basePath;
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
            var dirInfo = new DirectoryInfo(Path.GetDirectoryName(currPath));
            if(dirInfo.Name != "Mass Effect 3")
            {
                if (dirInfo.Parent == null)
                    return null;
                if (dirInfo.Parent.Name == "Mass Effect 3")
                    return dirInfo.Parent.FullName;
                if (dirInfo.Parent.Parent == null)
                    return null;
                currPath = FindME3ParentDir(dirInfo.Parent.FullName);
            }
            return currPath;
        }

        private void CreatingSettings()
        {
            Settings.Clear();
            //Mod
            Settings.Add(new ModSetting(29436, "ModWARBeta", false, 0, 1));
            Settings.Add(new ModSetting(29415, "ModQP", true, 0, 0));
            Settings.Add(new ModSetting(29440, "ModAssign", false, 0, 0));
            //Nor
            Settings.Add(new ModSetting(28902, "NorScanner", false, 0, 0));
            Settings.Add(new ModSetting(29338, "NorDocking", true, 1, 0));
            Settings.Add(new ModSetting(29338, "NorRelay", false, 0, 1));
            Settings.Add(new ModSetting(29339, "NorArm", true, 1, 0));
            Settings.Add(new ModSetting(28857, "NorRadio", true, 1, 0));
            Settings.Add(new ModSetting(28856, "NorCabinMus", true, 1, 0));
            //Squad
            Settings.Add(new ModSetting(28750, "Squad", true, 0, 0));
            //Mission DLC
            Settings.Add(new ModSetting(29330, "FromAshesDLC", false, 0, 0));
            Settings.Add(new ModSetting(29331, "LeviathanDLC", false, 0, 0));
            Settings.Add(new ModSetting(29332, "OmegaDLC", false, 0, 0));
            Settings.Add(new ModSetting(29333, "CitadelDLC", false, 0, 0));
            //Mission Main
            Settings.Add(new ModSetting(29337, "PrtyTuchanka", false, 0, 0));
            Settings.Add(new ModSetting(29335, "PrtyPerseus", false, 0, 0));
            Settings.Add(new ModSetting(29334, "PrtyThessia", false, 0, 0));
            //Mission N7
            Settings.Add(new ModSetting(29430, "N7Lab", false, 0, 0));
            Settings.Add(new ModSetting(29431, "N7benning", false, 0, 0));
            Settings.Add(new ModSetting(29432, "N7tuchanka", false, 0, 0));
            Settings.Add(new ModSetting(29433, "N7ontarom", false, 0, 0));
            Settings.Add(new ModSetting(29434, "N7noveria", false, 0, 0));
            Settings.Add(new ModSetting(29435, "N7kypladon", false, 0, 0));
            //Misc
            Settings.Add(new ModSetting(28824, "ArmAlliance", true, 0, 0));
            Settings.Add(new ModSetting(28819, "ArmAAP", true, 0, 0));
            Settings.Add(new ModSetting(28855, "CasGarrus", true, 1, 0));
            Settings.Add(new ModSetting(28870, "CasMiranda", true, 0, 0));
            //Mission ArkMod
            Settings.Add(new ModSetting(28650, "ArkN7Paladin", false, 0, 0));
        }
        #endregion

        #region iniReadWrite
        private void SaveSettings()
        {
            var instructions = new List<string>();
            foreach(var s in Settings)
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

            using (StreamWriter file = new StreamWriter(binPath))
            {
                foreach (string i in instructions)
                {
                    file.WriteLine(i);
                }
            }
            StatusText = "Settings file saved";
            needsSave = false;
        }
        private void LoadSettings()
        {
            var instructions = File.ReadAllLines(binPath);
            foreach(var i in instructions)
            {
                var valuestr = i.Substring(i.Length - 1, 1);
                Int32.TryParse(valuestr, out int value);
                var a = i.Remove(i.Length - 1, 1);
                var b = a.Replace(plotcmd, "");
                var c = b.Replace(intcmd, "");
                var d = c.Replace(boolcmd, "");
                if(Int32.TryParse(d, out int plotval));
                {
                    var setting = Settings.FirstOrDefault(f => f.PlotValue == plotval);
                    if(setting != null)
                    {
                        value = value - setting.OffsetValue;
                        string fieldname = $"{setting.VariableLink}_choice";
                        this.GetType().GetProperty(fieldname).SetValue(this, value, null);
                    }
                }
            }
            needsSave = false;
        }
        #endregion

        #region UserCommands
        private void MoveBackTab()
        {
            currentView--;
        }
        private void MoveNextTab()
        {
            currentView++;
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
                    mod_help_title.Text = ModAssign_TITLE;
                    mod_help_text.Text = ModAssign_TXT;
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
                case "NorRadio":
                    nor_help_title.Text = NorRadio_TITLE;
                    nor_help_text.Text = NorRadio_TXT;
                    break;
                case "NorCabMus":
                    nor_help_title.Text = NorCabinMus_TITLE;
                    nor_help_text.Text = NorCabinMus_TXT;
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
        private void Misc_MouseOver(object sender, MouseEventArgs e)
        {
            var source = e.OriginalSource as FrameworkElement;
            var tag = source.Tag.ToString();
            if (tag == displayedHelp)
                return;
            displayedHelp = tag;
            switch (tag)
            {
                case "ArmAll":
                    misc_help_title.Text = N7armAlliance_TITLE;
                    misc_help_text.Text = N7armAlliance_TXT;
                    break;
                case "ArmAAP":
                    misc_help_title.Text = N7armAAP_TITLE;
                    misc_help_text.Text = N7armAAP_TXT;
                    break;
                case "CasGar":
                    misc_help_title.Text = N7casgarrus_TITLE;
                    misc_help_text.Text = N7casgarrus_TXT;
                    break;
                case "CasMir":
                    misc_help_title.Text = N7casmiranda_TITLE;
                    misc_help_text.Text = N7casmiranda_TXT;
                    break;
                case "ArkPal":
                    misc_help_title.Text = ArkN7Paladin_TITLE;
                    misc_help_text.Text = ArkN7Paladin_TXT;
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
            storyMode_txt.Visibility = Visibility.Visible;
        }
        private void Squad_MouseLeft(object sender, MouseEventArgs e)
        {
            storyMode_txt.Visibility = Visibility.Collapsed;
            BonusSquad_txt.Visibility = Visibility.Visible;
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
