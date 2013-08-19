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

		public PlayerInfo()
		{
			Console.WriteLine("Loaded mod PlayerInfo");
		}

		public static string GetName()
		{
			return "PlayerInfo";
		}

		public static int GetVersion()
		{
			return 2; //Increased version number
		}

		public static MethodDefinition[] GetHooks(TypeDefinitionCollection scrollsTypes, int version)
		{
			try
			{
				return new MethodDefinition[] { 
                    // hook handleMessage in battlemode for the GameInfo message for getting the opponent name
                    scrollsTypes["BattleMode"].Methods.GetMethod("handleMessage", new Type[]{typeof(Message)}),
            	};
			}
			catch
			{
				return new MethodDefinition[] { };
			}
		}

		public override void BeforeInvoke(InvocationInfo info)
		{
			// we can obtain the BattleMode instance from this call
			if (info.targetMethod.Equals("handleMessage"))
			{
				if (bm == null)
				{
					bm = (BattleMode)info.target;
				}
				Message m = (Message)info.arguments[0];

				if (m is GameInfoMessage)
				{
					GameInfoMessage gm = (GameInfoMessage)m;

					if (new GameType(gm.gameType).isMultiplayer()) // just multiplayer matches
					{
						try
						{
							String opponentName = getOpponentName(gm);

							// now use the api to get the player's data
							WebClientTimeOut wc = new WebClientTimeOut();
							wc.TimeOut = 3000;
							wc.DownloadStringCompleted += (sender, e) =>
								{
									parseJSONResult(e.Result, opponentName);
								};
							wc.DownloadStringAsync(new Uri("http://a.scrollsguide.com/player?fields=all&name=" + opponentName));
						}
						catch // could not get information
						{

						}
					}
				}
			}

            return;
		}

        public override void AfterInvoke(InvocationInfo info, ref object returnValue)
        {
            return;
        }

		private String getOpponentName(GameInfoMessage gm)
		{
			return (gm.color == TileColor.white) ? gm.black : gm.white;
		}

		private void parseJSONResult(String result, String oppName)
		{
			// convert the html data to ApiResultMessage
			JsonReader reader = new JsonReader();
			Message message = reader.Read(result, System.Type.GetType("ApiResultMessage")) as ApiResultMessage;

			ApiResultMessage armsg = (ApiResultMessage)message;

			String chatMsg = "";

			if (armsg.msg.Equals("success")) // api call succeeded
			{
				chatMsg += "Player info: " + armsg.data.name + "\n";
				chatMsg += "Rank: " + armsg.data.rank + "\n";
				chatMsg += "Rating: " + armsg.data.rating + "\n";
				chatMsg += "Games played: " + armsg.data.played + "\n";
				chatMsg += "Games won: " + armsg.data.won + " (" + Math.Round(((float)armsg.data.won / (float)armsg.data.played) * 100) + "%)\n";
			}

			else
			{
				chatMsg = "Couldn't get data for " + oppName + ".";
			}

			MethodInfo mi = typeof(BattleMode).GetMethod("updateChat", BindingFlags.NonPublic | BindingFlags.Instance);

			if (mi != null) // send chat message
			{
				mi.Invoke(bm, new String[] { chatMsg });
			}

			else // can't invoke updateChat
			{
			}
		}
	}
}