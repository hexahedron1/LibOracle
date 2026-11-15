
namespace LibOracle.ModIntegrations;

public class CrsIntegration {
    public static void LoadEventsFromFile(Conversation self, string fileName, SlugcatStats.Name saveFile = null, bool oneRandomLine = false, int randomSeed = 0) {
        CustomRegions.Collectables.CustomConvo.LoadEventsFromFile(self, fileName, saveFile, oneRandomLine, randomSeed);
    }
    /*
    public static bool ConversationFileExists(Conversation.IOwnAConversation owner, string fileName, SlugcatStats.Name saveFile, Oracle.OracleID id) {
        return File.Exists(CustomRegions.Collectables.CustomConvo.SearchConvoFile(owner, fileName, saveFile, out _, id));
    }*/
}