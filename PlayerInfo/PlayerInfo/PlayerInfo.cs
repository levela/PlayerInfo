using Mono.Cecil;
using ScrollsModLoader.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Reflection;

using JsonFx.Json;

namespace PlayerInfo
{
    public class PlayerInfo : BaseMod
    {

        private BattleMode bm = null; // battlemode used for sending the chat
        private StreamWriter log;

        public static Boolean loaded = false;

        public PlayerInfo()
        {
        }

        ~PlayerInfo()
        {
            closeLog();
        }

        private void closeLog()
        {
            log.Flush();
            log.Close();
        }

        public override string GetName()
        {
            return "PlayerInfo";
        }

        public override int GetVersion()
        {
            return 1;
        }

        public override MethodDefinition[] GetHooks(TypeDefinitionCollection scrollsTypes, int version)
        {
            return new MethodDefinition[] { 
                    // hook handleMessage in battlemode for the GameInfo message for getting the opponent name
                    scrollsTypes["BattleMode"].Methods.GetMethod("handleMessage", new Type[]{typeof(Message)}),
                    // hook addListener in Communicator to obtain instance of BattleMode
                    scrollsTypes["Communicator"].Methods.GetMethod("addListener", new Type[]{typeof(ICommListener)})
            };
        }

        public override void Init()
        {
            if (!PlayerInfo.loaded)
            {
                try
                {
                    log = File.CreateText("PlayerInfo.log");
                    log.AutoFlush = true;
                }
                catch (IOException e)
                {
                    Console.WriteLine(e);
                }

                Console.WriteLine("Loaded mod PlayerInfo");

                PlayerInfo.loaded = true;
            }
        }

        public override bool BeforeInvoke(InvocationInfo info, out object returnValue)
        {
            // we can obtain the BattleMode instance from this call
            if (info.TargetMethod().Equals("addListener"))
            {
                if (info.Arguments()[0] is BattleMode)
                {
                    bm = (BattleMode)info.Arguments()[0];
                }
            }
            else if (bm != null && info.TargetMethod().Equals("handleMessage")) // no need to try without a battlemode instance for chat
            {
                Message m = (Message)info.Arguments()[0];

                if (m is GameInfoMessage)
                {
                    GameInfoMessage gm = (GameInfoMessage)m;

                    if (new GameType(gm.gameType).isMultiplayer()) // just multiplayer matches
                    {
                        log.WriteLine("Current color: " + gm.color);

                        String opponentName = getOpponentName(gm);
                        log.WriteLine("Battling against: " + opponentName);

                        // now use the api to get the player's data
                        String html = new WebClient().DownloadString("http://a.scrollsguide.com/player?fields=all&name=" + opponentName);
                        log.WriteLine(html);

                        // convert the html data to ApiResultMessage
                        JsonReader reader = new JsonReader();
                        Message message = reader.Read(html, System.Type.GetType("ApiResultMessage")) as ApiResultMessage;

                        ApiResultMessage armsg = (ApiResultMessage)message;

                        String chatMsg = "";
                        if (armsg.msg.Equals("success")) // api call succeeded
                        {
                            chatMsg += "Player info: " + armsg.data.name + "\n";
                            chatMsg += "Rank: " + armsg.data.rank + "\n";
                            chatMsg += "Rating: " + armsg.data.rating + "\n";
                            chatMsg += "Games played: " + armsg.data.played + "\n";
                            chatMsg += "Games won: " + armsg.data.won + "\n";
                        }
                        else
                        {
                            chatMsg = "Couldn't get data for " + opponentName + ".";
                        }
                        MethodInfo mi = typeof(BattleMode).GetMethod("updateChat", BindingFlags.NonPublic | BindingFlags.Instance);

                        if (mi != null)
                        { // send chat message
                            mi.Invoke(bm, new String[] { chatMsg });
                        }
                        else
                        {
                            log.WriteLine("Can't invoke updateChat");
                        }
                    }
                }
            }

            returnValue = null;
            return false;
        }

        public override void AfterInvoke(InvocationInfo info, ref object returnValue)
        {
            return;
        }

        private String getOpponentName(GameInfoMessage gm)
        {
            return (gm.color == TileColor.white) ? gm.black : gm.white;
        }
    }
}
