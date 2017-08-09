using System;
using System.IO;
using System.Threading;
using System.Security.Cryptography;

using DiscordBot.Scripts.Commands;

using SteamKit2;
using System.Windows.Forms;

namespace DiscordBot.Scripts.Steam {
    public static class SteamController {
        public static readonly string authPath = "Data/steamauth.bin";

        static string emailAuth;
        static string twoFactorAuth;

        public static SteamClient steamClient;
        public static SteamUser steamUser;
        public static CallbackManager steamManager;
        public static SteamFriends steamFriends;

        public static Thread thread;
        
        static bool running;
        static bool firstAttempt = true;

        public static string startupError = string.Empty;

        public static void Start() {
            if(steamClient == null) {
                steamClient = new SteamClient();
                steamManager = new CallbackManager(steamClient);

                steamUser = steamClient.GetHandler<SteamUser>();
                steamFriends = steamClient.GetHandler<SteamFriends>();

                steamManager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
                steamManager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);
                steamManager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
                steamManager.Subscribe<SteamUser.LoggedOffCallback>(OnLoggedOff);
                steamManager.Subscribe<SteamUser.UpdateMachineAuthCallback>(OnMachineAuth);
                steamManager.Subscribe<SteamUser.AccountInfoCallback>(OnAccountInfo);
                steamManager.Subscribe<SteamFriends.FriendMsgCallback>(OnChatMsg);
            }

            if(thread != null && thread.IsAlive) {
                thread.Abort();
                thread.Join();
            }

            thread = new Thread(Connect);
            thread.Start();
        }

        public static void Connect() {
            if(firstAttempt)
                Console.WriteLine("\n[Steam] Connecting...");

            steamClient.Connect();

            running = true;
            while(running) {
                steamManager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
            }
        }

        static void OnConnected(SteamClient.ConnectedCallback e) {
            if(e.Result != EResult.OK) {
                running = false;
                Console.WriteLine("[Steam] Connection failed.");
                /*DialogResult r = MessageBox.Show("Could not connect to Steam.", "Error", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error, MessageBoxDefaultButton.Button2);
                if(r == DialogResult.Retry)
                    Connect();
                else
                    SteamForm.EnableForm(true);*/
                return;
            }
            if(firstAttempt) 
                firstAttempt = false;

            byte[] sentryHash = null;
            if(File.Exists(authPath))
                sentryHash = CryptoHelper.SHAHash(File.ReadAllBytes(authPath));

            Console.WriteLine("[Steam] Attempting to logging in...");
            steamUser.LogOn(new SteamUser.LogOnDetails {
                Username = SteamForm.SteamUsername,
                Password = SteamForm.SteamPassword,
                AuthCode = emailAuth,
                TwoFactorCode = twoFactorAuth,
                SentryFileHash = sentryHash
            });
        }

        static void OnDisconnected(SteamClient.DisconnectedCallback e) {
            running = false;

            //if(Configuration.debugMode)
            //    Console.WriteLine("SteamController.OnDisconnect");
        }
        
        static async void OnLoggedOn(SteamUser.LoggedOnCallback e) {
            if(e.Result == EResult.AccountLogonDenied) {
                SteamAuthForm form = await SteamAuthForm.WaitForCode($"Enter the auth code sent to {e.EmailDomain}:");
                if(form.response == Forms.AuthResponse.Connect) {
                    emailAuth = form.output;
                    Connect();
                    return;
                }
                //Console.Write("Enter the auth code sent to " + e.EmailDomain + ": ");
                //emailAuth = Console.ReadLine();
                steamClient.Disconnect();
                SteamForm.EnableForm(true);
                return;
            }
            else if(e.Result == EResult.AccountLoginDeniedNeedTwoFactor) {
                SteamAuthForm form = await SteamAuthForm.WaitForCode($"Enter the auth code from your Mobile Steam App:");
                if(form.response == Forms.AuthResponse.Connect) {
                    twoFactorAuth = form.output;
                    Connect();
                    return;
                }
                //Console.Write("Enter the auth code from your Mobile Steam App: ");
                //twoFactorAuth = Console.ReadLine();
                steamClient.Disconnect();
                SteamForm.EnableForm(true);
                return;
            }
            else if(e.Result == EResult.TwoFactorCodeMismatch) {
                SteamAuthForm form = await SteamAuthForm.WaitForCode($"Enter the CORRECT auth code from your Mobile Steam App:");
                if(form.response == Forms.AuthResponse.Connect) {
                    twoFactorAuth = form.output;
                    Connect();
                    return;
                }
                //Console.Write("Enter the CORRECT auth code from your Mobile Steam App: ");
                //twoFactorAuth = Console.ReadLine();
                steamClient.Disconnect();
                SteamForm.EnableForm(true);
                return;
            }
            else if(e.Result != EResult.OK) {
                string developerNote = string.Empty;
                if(e.Result == EResult.InvalidPassword)
                    developerNote = "\n\nThis can also mean 'There have been too many login failures from your network in a short time period. Please wait and try again later.'";
                MessageBox.Show($"Could not connect to Steam.\n\nResult: {e.Result}\nExtended: {e.ExtendedResult}{developerNote}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine("Could not connect to Steam (" + e.Result + ").");
                Console.WriteLine("e.ExtendedResult = " + e.ExtendedResult);
                //reconnect = true;
                steamClient.Disconnect();
                SteamForm.EnableForm(true);
                return;
            }
            
            Console.WriteLine("[Steam] Successfully logged on.");
            SteamForm.instance.InvokeClose();
        }

        static void OnLoggedOff(SteamUser.LoggedOffCallback e) {
            //if(Settings.DebugMode)
            //    Console.WriteLine("SteamController.OnLoggedOff");
        }

        static void OnMachineAuth(SteamUser.UpdateMachineAuthCallback callback) {
            Console.WriteLine("[Steam] Updating authentication file...");

            int fileSize;
            byte[] sentryHash;
            using(var fs = File.Open(authPath, FileMode.OpenOrCreate, FileAccess.ReadWrite)) {
                fs.Seek(callback.Offset, SeekOrigin.Begin);
                fs.Write(callback.Data, 0, callback.BytesToWrite);
                fileSize = (int)fs.Length;

                fs.Seek(0, SeekOrigin.Begin);
                using(var sha = new SHA1CryptoServiceProvider()) {
                    sentryHash = sha.ComputeHash(fs);
                }
            }
            
            steamUser.SendMachineAuthResponse(new SteamUser.MachineAuthDetails {
                JobID = callback.JobID,

                FileName = callback.FileName,

                BytesWritten = callback.BytesToWrite,
                FileSize = fileSize,
                Offset = callback.Offset,

                Result = EResult.OK,
                LastError = 0,

                OneTimePassword = callback.OneTimePassword,

                SentryFileHash = sentryHash,
            });
        }

        public static void OnAccountInfo(SteamUser.AccountInfoCallback e) {
            steamFriends.SetPersonaState(EPersonaState.Online);
        }

        public static void OnChatMsg(SteamFriends.FriendMsgCallback e) {
            if(e.EntryType != EChatEntryType.ChatMsg)
                return;
            if(e.Message.IndexOf(Settings.SteamCommandPrefix) != 0)
                return;
            /*if(SteamPermission.Get(e.Sender.GetStaticAccountKey()) == null) {
                steamFriends.SendChatMessage(e.Sender, EChatEntryType.ChatMsg, Language.SteamNotLinked);
                return;
            }*/

            Users.SteamUser user = new Users.SteamUser(e.Sender);
            user.name = steamFriends.GetFriendPersonaName(e.Sender);

            Command.Run(user, e.Message.Substring(Settings.SteamCommandPrefix.Length));
        }
    }
}
