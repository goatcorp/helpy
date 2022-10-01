using Google.Protobuf;
using System.Text.Json;
using Yarn.Compiler;

var scriptFolder = new DirectoryInfo(args[0]);
var scriptFiles = scriptFolder.GetFiles("*.yarn");

/* this is buggy in the yarn we have
foreach (var file in scriptFiles)
{
    var text = File.ReadAllText(file.FullName);
    text = Utility.AddTagsToLines(text);
    File.WriteAllText(file.FullName, text);
}
*/

var job = CompilationJob.CreateFromFiles(scriptFiles.Select(x => x.FullName));

var result = Compiler.Compile(job);
File.WriteAllBytes(Path.Combine(scriptFolder.FullName, "program.yarnc"), result.Program.ToByteArray());

File.WriteAllText(Path.Combine(scriptFolder.FullName, "strings.json"), JsonSerializer.Serialize(result.StringTable.ToDictionary(x => x.Key, x => x.Value.text)));
File.WriteAllText(Path.Combine(scriptFolder.FullName, "decl.json"), JsonSerializer.Serialize(result.Declarations));


Console.WriteLine($"Ok, compiled");