using System.Threading;
using Dalamud.Game.Text;
using Dalamud.Plugin.Services;

namespace EndEncounter
{
    public static class ACTHelper
    {
        public static void EndACTEncounter(IChatGui chatGui)
        {
            if (chatGui == null) return;

            chatGui.Print(new XivChatEntry()
            {
                Type = XivChatType.Echo,
                Message = "end"
            });
        }

        public static void EndACTEncounter(IChatGui chatGui, int waitTime)
        {
            Thread.Sleep(waitTime);

            EndACTEncounter(chatGui);
        }
    }
}
