using System.Net.Http.Json;
using Yarn;

namespace Helpy.Shared.Conversation
{
    public class ConversationManager
    {
        private Dialogue? dialogue;
        private Dictionary<string, string>? stringTable;
        private Dictionary<string, string>? linkTable;

        public abstract class Message
        {
            public Message(bool isUser)
            {
                IsUser = isUser;
            }
            
            public bool IsUser { get; private set; }
        }
        
        public class TextMessage : Message
        {
            public TextMessage(string text, bool isUser) : base(isUser)
            {
                Text = text;
            }

            public string Text { get; private set; }
        }
        
        public class ImageMessage : Message
        {
            public ImageMessage(string src, string alt, bool isUser) : base(isUser)
            {
                Src = src;
                Alt = alt;
            }

            public string Src { get; private set; }
            
            public string Alt { get; private set; }
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
            linkTable = await client.GetFromJsonAsync<Dictionary<string, string>>("flow/links.json");

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

            var markup = dialogue!.ParseMarkup(lineStr);
            var rawLine = markup.Text;
            string? imageLink = null;
            foreach (var attrib in markup.Attributes)
            {
                var text = markup.TextForAttribute(attrib);
                string? newText = null;

                switch (attrib.Name)
                {
                    case "button":
                    case "link":
                    {
                        var linkKey = attrib.Properties["href"].StringValue;
                        if (!linkTable!.TryGetValue(linkKey, out var linkHref))
                            throw new Exception($"Could not find link in link table: {linkKey}");

                        newText = $"<a href=\"{linkHref}\">{text}</a>";
                    }
                        break;
                    case "img":
                    {
                        var linkKey = attrib.Properties["src"].StringValue;
                        if (!linkTable!.TryGetValue(linkKey, out var linkHref))
                            throw new Exception($"Could not find img src in link table: {linkKey}");

                        newText = string.Empty;
                        imageLink = linkHref;
                    }
                        break;
                    default:
                        Console.WriteLine("Unknown attribute: {0}", attrib.Name);
                        break;
                }

                if (newText != null)
                {
                    rawLine = rawLine.Remove(attrib.Position, attrib.Length).Insert(attrib.Position, newText);
                }
            }

            if (!string.IsNullOrEmpty(rawLine)) 
                Messages.Add(new TextMessage(rawLine, false));
            
            if (imageLink != null)
                Messages.Add(new ImageMessage(imageLink, "aaa", false));
                
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
            Messages.Add(new TextMessage(Choices![option], true));

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
