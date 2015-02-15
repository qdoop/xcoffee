using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Media;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;
using Intel = Microsoft.VisualStudio.Language.Intellisense;

namespace qdoop.EditorExtensions.CoffeeScript
{
    [Export(typeof(ICompletionSourceProvider))]
    [ContentType("CoffeeScript")]
    [Name("CoffeeScriptCompletion")]
    class CoffeeCompletionSourceProvider : ICompletionSourceProvider
    {
        public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer)
        {
            return new CoffeeCompletionSource(textBuffer);
        }
    }

    class CoffeeCompletionSource : ICompletionSource
    {
        private ITextBuffer _buffer;
        private bool _disposed = false;
        private static ImageSource _glyph = GlyphService.GetGlyph(StandardGlyphGroup.GlyphGroupVariable, StandardGlyphItem.GlyphItemPublic);

        public CoffeeCompletionSource(ITextBuffer buffer)
        {
            _buffer = buffer;
        }

        public void AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
        {
            if (_disposed)
                return;

            ITextSnapshot snapshot = _buffer.CurrentSnapshot;
            var triggerPoint = session.GetTriggerPoint(snapshot);
            if (triggerPoint == null)
                return;

            List<Intel.Completion> completions = new List<Intel.Completion>();
            var hset = new HashSet<string>() { 
                "require('')", 
                "module.exports", 
                "console.log ", 
                
                "()->" ,
            };


            var kwds =new HashSet<string>() {
                                        "and", 
 									   "break", 
 									   "by", 
									   "catch", 
 									   "class", 
 									   "continue", 
                                        "debugger", 
                                        "delete", 
 									   "do", 
 									   "else", 
 									   "extends", 
 									   "false", 
 									   "finally", 
 									   "for", 
 									   "if", 
 									   "in", 
                                        "instanceof", 
 									   "is", 
 									   "isnt", 
                                        "loop", 
 									   "new", 
 									   "no", 
 									   "not", 
 									   "null", 
 									   "of", 
 									   "off", 
 									   "on", 
 									   "or", 
 									   "return", 
 									   "super", 
 									   "switch", 
 									   "then", 
 									   "this", 
 									   "throw", 
 									   "true", 
 									   "try", 
                                        "typeof", 
 									   "undefined", 
                                        "unless", 
 									   "until", 
 									   "when", 
 									   "while", 
 									   "yes",};


            var txt = snapshot.GetText();
            var tks = txt.Split(new System.Char[] { ' ', '@', '=', '[', ']','{','}', '(',')', '+', '-', '/', '*', ',', '.', ':', '\t', '\r', '\n' });
    
            
            foreach (string item in tks)
            {
                if ("" != item.Trim() && 1<item.Length) hset.Add(item);
            }

            foreach (string item in kwds)
            {
                hset.Add(item);
            }

            foreach (string item in hset)
            {
                completions.Add(new Intel.Completion(item, item, null, _glyph, item));
            }


            var line = triggerPoint.Value.GetContainingLine();
            SnapshotPoint start = triggerPoint.Value;
            string text = line.GetText();

            //int index = text.IndexOf(':');
            //int hash = text.IndexOf('#');
            //if (hash > -1 && hash < triggerPoint.Value.Position || (index > -1 && (start - line.Start.Position) > index))
            //    return;

            if ("" == text.Trim()) return;

            //while ( start > line.Start
            //    && !char.IsWhiteSpace((start - 1).GetChar()) 
            //    && -1 == "`'#=+-*/,.[]{}()<>?$%^&@!~\"\t\r\n ".IndexOf((start - 1).GetChar())
            //    )
            while(  line.Start<start && (char.IsLetterOrDigit((start - 1).GetChar()) || -1<"$_".IndexOf((start - 1).GetChar())) )
            {
                start -= 1;
            }

            var applicableTo = snapshot.CreateTrackingSpan(new SnapshotSpan(start, triggerPoint.Value), SpanTrackingMode.EdgeInclusive);

            completionSets.Add(new CompletionSet("All", "All", applicableTo, completions, Enumerable.Empty<Intel.Completion>()));
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}