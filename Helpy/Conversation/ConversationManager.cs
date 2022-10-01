using System.Net.Http.Json;
using Yarn;

namespace Helpy.Conversation
{
    public class ConversationManager
    {
        private Dialogue? dialogue;
        private Dictionary<string, string>? stringTable;

        public class Message
        {
            public Message(string text, bool isUser)
            {
                Text = text;
                IsUser = isUser;
            }

            public string Text { get; private set; }

            public bool IsUser { get; private set; }
        }

        public List<Message> Messages { get; private set; } = new List<Message>();

        public IReadOnlyDictionary<int, string>? Choices;

        public enum ConversationState
        {
            Default,
            Choicer,
            UploadTsPack,
            Finish
        }

        public ConversationState State { get; private set; }

        public TroubleContext? Trouble { get; private set; }

        public async Task LoadAndInit(HttpClient client)
        {
            Messages.Clear();
            State = ConversationState.Default;

            stringTable = await client.GetFromJsonAsync<Dictionary<string, string>>("flow/strings.json");

            var script = await client.GetByteArrayAsync("flow/program.yarnc");
            var program = Yarn.Program.Parser.ParseFrom(script);

            var storage = new MemoryVariableStore();
            dialogue = new Dialogue(storage);

            Trouble = new TroubleContext();

            var library = new HelpyLibrary(Trouble);
            dialogue.Library.ImportLibrary(library);

            dialogue.LineHandler += HandleLine;
            dialogue.CommandHandler += HandleCommand;
            dialogue.OptionsHandler += HandleOptions;
            dialogue.NodeCompleteHandler += HandleNodeComplete;
            dialogue.DialogueCompleteHandler += HandleDialogueComplete;

            dialogue.LogErrorMessage += a => Console.WriteLine($"[YARN] {a}");
            dialogue.LogDebugMessage += a => Console.WriteLine($"[YARN] {a}");

            dialogue.SetProgram(program);
            dialogue.SetNode("Start");

            dialogue.Continue();
        }

        private void HandleLine(Line line)
        {
            var lineStr = this.stringTable![line.ID];
            Console.WriteLine(lineStr);

            Messages.Add(new(lineStr, false));

            if (State != ConversationState.Finish)
                dialogue!.Continue();
        }

        private string GetLocalized(string lineId)
        {
            return this.stringTable![lineId];
        }

        private void HandleCommand(Command command)
        {
            switch(command.Text)
            {
                case "UploadTspack":
                    {
                        State = ConversationState.UploadTsPack;
                    }
                    break;
                default:
                    Console.WriteLine("Can't find command: {0}", command.Text);
                    dialogue!.Continue();
                    break;
            }
        }

        private void HandleOptions(OptionSet set)
        {
            State = ConversationState.Choicer;
            Choices = set.Options
                .Where(x => x.IsAvailable)
                .ToDictionary(x => x.ID, x => GetLocalized(x.Line.ID));

            foreach(var option in set.Options)
            {
                var lineStr = this.stringTable![option.Line.ID];

                Console.WriteLine($"{option.ID} - {lineStr}");
            }
        }

        private void HandleNodeComplete(string completedNodeName)
        {
            //dialogue!.Continue();
        }

        private void HandleDialogueComplete()
        {
            State = ConversationState.Finish;
            Console.WriteLine("HandleDialogueComplete");
        }

        public void ChoicerChoose(int option)
        {
            Messages.Add(new(Choices![option], true));

            dialogue!.SetSelectedOption(option);
            State = ConversationState.Default;
            Choices = null;

            dialogue!.Continue();
        }

        public void LoadTsPack(byte[] data)
        {
            Trouble!.LoadPack(data);
            State = ConversationState.Default;

            dialogue!.Continue();
        }
    }
}
