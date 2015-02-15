using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace qdoop.EditorExtensions.CoffeeScript
{
    #region Command Filter

    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType("CoffeeScript")]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    internal sealed class VsTextViewCreationListener : IVsTextViewCreationListener
    {
        [Import]
        IVsEditorAdaptersFactoryService AdaptersFactory = null;

        [Import]
        ICompletionBroker CompletionBroker = null;

        [Import]
        ITextDocumentFactoryService TextDocumentFactoryService = null;

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            IWpfTextView view = AdaptersFactory.GetWpfTextView(textViewAdapter);
            Debug.Assert(view != null);

            ITextDocument document;
            if (!TextDocumentFactoryService.TryGetTextDocument(view.TextDataModel.DocumentBuffer, out document))
                return;

            CommandFilter filter = new CommandFilter(view, CompletionBroker);

            IOleCommandTarget next;
            ErrorHandler.ThrowOnFailure(textViewAdapter.AddCommandFilter(filter, out next));
            filter.Next = next;
        }
    }

    internal sealed class CommandFilter : IOleCommandTarget
    {
        private ICompletionSession _currentSession;

        public CommandFilter(IWpfTextView textView, ICompletionBroker broker)
        {
            _currentSession = null;

            TextView = textView;
            Broker = broker;
        }

        public IWpfTextView TextView { get; private set; }
        public ICompletionBroker Broker { get; private set; }
        public IOleCommandTarget Next { get; set; }

        private static char GetTypeChar(IntPtr pvaIn)
        {
            return (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            //Debug.Print("char=" + nCmdID);

            if (pguidCmdGroup != VSConstants.VSStd2K)
                return Next.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

            bool handled = false;
            int hresult = VSConstants.S_OK;
            
            char ch = char.MinValue;
            if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.TYPECHAR)            
                ch=GetTypeChar(pvaIn);

            //check for a commit character 
            if (   nCmdID == (uint)VSConstants.VSStd2KCmdID.RETURN
                || nCmdID == (uint)VSConstants.VSStd2KCmdID.TAB
                || char.IsWhiteSpace(ch) 
                //|| char.IsPunctuation(ch)
                )
            {
                //check for a a selection 
                if (_currentSession != null && !_currentSession.IsDismissed)
                {
                    //if the selection is fully selected, commit the current session 
                    if (_currentSession.SelectedCompletionSet.SelectionStatus.IsSelected)
                    {
                        _currentSession.Commit();
                        //also, don't add the character to the buffer 
                        return VSConstants.S_OK;
                    }
                    else
                    {
                        //if there is no selection, dismiss the session
                        _currentSession.Dismiss();
                    }
                }
            }

            //pass along the command so the char is added to the buffer 
            hresult=Next.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

            if (!ch.Equals(char.MinValue) && ( char.IsLetterOrDigit(ch)||-1<"$_".IndexOf(ch) ))
            {
                if (_currentSession == null || _currentSession.IsDismissed) // If there is no active session, bring up completion
                {
                    this.StartSession();
                    _currentSession.Filter();
                }
                else     //the completion session is already active, so just filter
                {
                    _currentSession.Filter();
                }

                return VSConstants.S_OK;
            }


            if (   nCmdID == (uint)VSConstants.VSStd2KCmdID.BACKSPACE   //redo the filter if there is a deletion
                || nCmdID == (uint)VSConstants.VSStd2KCmdID.DELETE)
            {
                if (_currentSession != null && !_currentSession.IsDismissed)
                    _currentSession.Filter();

                return VSConstants.S_OK;
            }
            
            return hresult;

            /////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //// 1. Pre-process
            //if (pguidCmdGroup == VSConstants.VSStd2K)
            //{
            //    switch ((VSConstants.VSStd2KCmdID)nCmdID)
            //    {
            //        case VSConstants.VSStd2KCmdID.AUTOCOMPLETE:
            //        case VSConstants.VSStd2KCmdID.COMPLETEWORD:
            //            handled = StartSession();
            //            break;
            //        case VSConstants.VSStd2KCmdID.RETURN:
            //            //handled = Complete(false);
            //            //Cancel();
            //            //if (null!=_currentSession)
            //            //{
            //            //    _currentSession.Dismiss();
            //            //    _currentSession = null;
            //            //}

            //            handled = Complete(true);
            //            break;
            //        case VSConstants.VSStd2KCmdID.TAB:
            //            handled = Complete(true);
            //            break;
            //        case VSConstants.VSStd2KCmdID.CANCEL:
            //            handled = Cancel();
            //            break;
            //    }
            //}

            //if (!handled)
            //    hresult = Next.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

            //if (ErrorHandler.Succeeded(hresult))
            //{
            //    if (pguidCmdGroup == VSConstants.VSStd2K)
            //    {
            //        switch ((VSConstants.VSStd2KCmdID)nCmdID)
            //        {
            //            //case VSConstants.VSStd2KCmdID.RETURN:
            //            //    Cancel();
            //            //    break;
            //            case VSConstants.VSStd2KCmdID.TYPECHAR:
            //                //char ch = GetTypeChar(pvaIn);
            //                //if (-1 < "`'#=+-*/,.[]{}()<>?$%^&@!~\"\t\r\n ".IndexOf(ch))
            //                //    Cancel();
                            
            //                ////else 
            //                //if (char.IsLetterOrDigit(ch))//(!char.IsPunctuation(ch) && !char.IsControl(ch))
            //                //    StartSession();
            //                ////else if (_currentSession != null)

            //                if (char.IsLetterOrDigit(ch))
            //                    StartSession();
            //                else
            //                    Cancel();

            //                Filter();
            //                break;
            //            case VSConstants.VSStd2KCmdID.BACKSPACE:
            //                //if (_currentSession == null)
            //                StartSession();
            //                Filter();
            //                break;
            //        }
            //    }
            //}

            //return hresult;
        }

        private void Filter()
        {
            if (_currentSession == null)
                return;

            _currentSession.SelectedCompletionSet.SelectBestMatch();
            _currentSession.SelectedCompletionSet.Recalculate();
        }

        bool Cancel()
        {
            if (_currentSession == null)
                return false;

            _currentSession.Dismiss();

            return true;
        }

        bool Complete(bool force)
        {
            if (_currentSession == null)
                return false;

            if (!_currentSession.SelectedCompletionSet.SelectionStatus.IsSelected && !force)
            {
                _currentSession.Dismiss();
                return false;
            }
            else
            {
                _currentSession.Commit();
                return true;
            }
        }

        bool StartSession()
        {
            if (_currentSession != null)
                return false;

            SnapshotPoint caret = TextView.Caret.Position.BufferPosition;
            ITextSnapshot snapshot = caret.Snapshot;

            if (!Broker.IsCompletionActive(TextView))
            {
                _currentSession = Broker.CreateCompletionSession(TextView, snapshot.CreateTrackingPoint(caret, PointTrackingMode.Positive), true);
            }
            else
            {
                _currentSession = Broker.GetSessions(TextView)[0];
            }
            _currentSession.Dismissed += (sender, args) => _currentSession = null;

            if (!_currentSession.IsStarted)
                _currentSession.Start();

            return true;
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            if (pguidCmdGroup == VSConstants.VSStd2K)
            {
                switch ((VSConstants.VSStd2KCmdID)prgCmds[0].cmdID)
                {
                    case VSConstants.VSStd2KCmdID.AUTOCOMPLETE:
                    case VSConstants.VSStd2KCmdID.COMPLETEWORD:
                        prgCmds[0].cmdf = (uint)OLECMDF.OLECMDF_ENABLED | (uint)OLECMDF.OLECMDF_SUPPORTED;
                        return VSConstants.S_OK;
                }
            }
            return Next.QueryStatus(pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }
    }

    #endregion
}