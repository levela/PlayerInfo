using Mono.Cecil;
using ScrollsModLoader.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

using JsonFx.Json;

namespace PlayerInfo
{
    public class PlayerInfo : BaseMod
    {

        private StreamWriter log;

        public PlayerInfo()
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
        }

        ~PlayerInfo()
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
            return new MethodDefinition[] { scrollsTypes["BattleMode"].Methods.GetMethod("handleMessage", new Type[]{typeof(Message)}) };
        }

        public override void Init()
        {
        }

        public override bool BeforeInvoke(InvocationInfo info, out object returnValue)
        {
            Message m = (Message)info.Arguments()[0];

            if (m is GameInfoMessage)
            {
                GameInfoMessage gm = (GameInfoMessage)m;
                log.WriteLine(gm.ToString());

                //if (new GameType(gm.gameType).isMultiplayer())
               // {
                    log.WriteLine("Current color: " + gm.color);

                    String opponentName = getOpponentName(gm);
                    log.WriteLine("Battling against: " + opponentName);

                    String html = new WebClient().DownloadString("http://a.scrollsguide.com/player?name=kbasten&fields=all&d");
                    log.WriteLine(html);

                    JsonReader reader = new JsonReader();
                    Message message = reader.Read(html, System.Type.GetType("ApiResultMessage")) as ApiResultMessage;

                    ApiResultMessage armsg = (ApiResultMessage)message;
                    log.WriteLine("Name: " + armsg.data.name);
                    log.WriteLine("Rank: " + armsg.data.rank);
                    log.WriteLine("Rating: " + armsg.data.rating);
                //}
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
