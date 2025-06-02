const string DoubleSlashCommentPrefix = "// ";
const string TripleSlashCommentPrefix = "/// ";
const string SummaryStart = "/// <summary>";
const string SummaryEnd = "/// </summary>";
const string FileFilter = "*.cs";

var folderPath = @"..\..\..\..\..\src\Box2D.NET\";

// Tests: B2BodySim, B2World, B2WorldId
var files = Directory.GetFiles(folderPath, FileFilter, SearchOption.AllDirectories)
    //.Where(w => w.Contains("B2WorldId"))
    //.Take(50)
    .ToList();

Console.WriteLine($"Found {files.Count} C# files in the folder: {folderPath}");

foreach (var filePath in files)
{
    await ProcessFileAsync(filePath);
}

Console.WriteLine("*** Processing completed ***");

async Task ProcessFileAsync(string filePath)
{
    Console.WriteLine($"\nProcessing file: {filePath}");

    var content = await File.ReadAllTextAsync(filePath);
    var lines = content.Split(["\r\n", "\n"], StringSplitOptions.None).ToList();
    var commentLineIndexes = ExtractCommentLineIndexes(lines);

    RemovePreNamespaceComments(lines, commentLineIndexes);

    if (commentLineIndexes.Count == 0)
    {
        Console.WriteLine("No comment lines found in the file.");
        return;
    }

    var commentBlocks = ExtractCommentBlocks(lines, commentLineIndexes);

    if (commentBlocks.Count == 0)
    {
        Console.WriteLine("No comment blocks found.");
        return;
    }

    ConvertCommentsToTripleSlash(lines, commentBlocks);

    WrapCommentsWithSummaryTags(lines, commentBlocks);

    //File.WriteAllText(filePath.Replace(".cs", ".xmlcomments.cs"), string.Join(Environment.NewLine, lines));
    File.WriteAllText(filePath, string.Join(Environment.NewLine, lines));

    Console.WriteLine($"Output written to: {filePath}");
}

static void RemovePreNamespaceComments(List<string> lines, List<int> commentLineIndexes)
{
    var namespaceLineIndex = lines.FindIndex(line => line.TrimStart().StartsWith("namespace "));

    commentLineIndexes.RemoveAll(index => index < namespaceLineIndex);
}

static List<CommentBlock> ExtractCommentBlocks(List<string> lines, List<int> commentLineIndexes)
{
    var commentBlocks = new List<CommentBlock>();
    var startIndex = commentLineIndexes[0];
    var endIndex = commentLineIndexes[0];

    if (commentLineIndexes.Count == 1)
    {
        AddBlockIfFollowedByPublic(startIndex, endIndex);

        return commentBlocks;
    }

    for (int i = 1; i < commentLineIndexes.Count; i++)
    {
        var nextIndex = commentLineIndexes[i];

        if (nextIndex - endIndex != 1)
        {
            AddBlockIfFollowedByPublic(startIndex, endIndex);
            startIndex = nextIndex;
        }

        endIndex = nextIndex;

        if (i == commentLineIndexes.Count - 1)
        {
            AddBlockIfFollowedByPublic(startIndex, endIndex);
        }
    }

    return commentBlocks;

    void AddBlockIfFollowedByPublic(int startIndex, int endIndex)
    {
        if (endIndex + 1 < lines.Count && lines[endIndex + 1].Contains("public"))
        {
            commentBlocks.Add(new CommentBlock(startIndex, endIndex));
        }
    }
}

static List<int> ExtractCommentLineIndexes(List<string> lines)
{
    HashSet<string> commentStarts = [DoubleSlashCommentPrefix, TripleSlashCommentPrefix];

    return lines
         .Select((line, index) => (line, index))
         .Where(item => commentStarts.Any(commentStart =>
             item.line.TrimStart().StartsWith(commentStart)))
         .Select(item => item.index)
         .ToList();
}

static void ConvertCommentsToTripleSlash(List<string> lines, List<CommentBlock> commentBlocks)
{
    foreach (var block in commentBlocks)
    {
        Console.WriteLine($"Comment block from {block.StartIndex + 1} to {block.EndIndex + 1} (length: {block.Length})");

        for (int i = 0; i < block.Length; i++)
        {
            int lineIndex = block.StartIndex + i;

            if (!lines[lineIndex].TrimStart().StartsWith(TripleSlashCommentPrefix))
            {
                lines[lineIndex] = lines[lineIndex].Replace(DoubleSlashCommentPrefix, "/// ");
            }
        }
    }
}

void WrapCommentsWithSummaryTags(List<string> lines, List<CommentBlock> commentBlocks)
{
    for (int i = commentBlocks.Count - 1; i >= 0; i--)
    {
        var block = commentBlocks[i];
        var indentation = GetIndentation(lines[block.StartIndex]);

        lines.Insert(block.EndIndex + 1, $"{indentation}{SummaryEnd}");
        lines.Insert(block.StartIndex, $"{indentation}{SummaryStart}");
    }

    string GetIndentation(string line) => new(' ', line.Length - line.TrimStart().Length);
}

record class CommentBlock(int StartIndex, int EndIndex)
{
    public int Length => EndIndex - StartIndex + 1;
}