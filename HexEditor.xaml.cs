using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//#if OS_WINDOWS
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
//#endif
using Microsoft.Maui.Platform;
using Microsoft.Maui.ApplicationModel;

namespace HexEditor
{
    public class BinaryRepresentation
    {
        public byte[] _Bytes { get; set; }
        public string _BytesAsString { get; set; }
        public string _BytesAsStringFormatted { get; set; }
        public UInt64 _PosBytes { get; set; }
        public UInt64 _PosBytesString { get; set; }
        public UInt64 _PosBytesStringFormatted { get; set; }
        public UInt64 _SelectedPosBytes { get; set; }
        public UInt64 _SelectedPosBytesString { get; set; }
        public UInt64 _SelectedPosBytesStringFormatted { get; set; }
    }

    public partial class HexEditor : ContentView
    {
        public HexEditor()
        {
            InitializeComponent();

            _originalFile = new BinaryRepresentation();
            _originalFile._Bytes = new byte[512];

            for (int i = 0; i < _originalFile._Bytes.Length; ++i)
            {
                _originalFile._Bytes[i] = (byte)i;
            }

            initUnchangedEditor();
            setOffsetArea();

            Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("CurrentOffset", (handler, view) =>
            {
                if (view is Entry)
                    handler.PlatformView.Padding = new Microsoft.UI.Xaml.Thickness(0, 0, 0, 0);
            });
        }

        private BinaryRepresentation _originalFile;

        private void initUnchangedEditor()
        {
            formatBytesString();
            HexEditOriginal.Text = _originalFile._BytesAsStringFormatted;

        }

        private void onEditorTapped(object sender, EventArgs e)
        {
            CurrentOffset.Text = (HexEditOriginal.CursorPosition / 3).ToString("X");
        }

        private void formatBytesString()
        {
            _originalFile._BytesAsString = "";
            _originalFile._BytesAsStringFormatted = "";

            for (int i = 0; i < _originalFile._Bytes.Length; ++i)
                _originalFile._BytesAsString += _originalFile._Bytes[i].ToString("X2");

            for (int i = 0; i < _originalFile._BytesAsString.Length; i += 2)
            {
                if ((i & 0x1F) == 0x1E && i > 0)
                    _originalFile._BytesAsStringFormatted += _originalFile._BytesAsString.Substring(i, 2) + "\n";
                else if (i == _originalFile._BytesAsString.Length - 1)
                    _originalFile._BytesAsStringFormatted += _originalFile._BytesAsString.Substring(i, 2);
                else
                    _originalFile._BytesAsStringFormatted += _originalFile._BytesAsString.Substring(i, 2) + " ";
            }
        }

        private void setOffsetArea()
        {
            OffsetList.Clear();

            for (int i = 0; i <= 0x1F0; i += 0x10)
            {
                Label temp = new Label();
                temp.Text = i.ToString("X2");
                temp.TextColor = new Color(0xCC, 0xCC, 0xFF);
                temp.FontFamily = "Consolas";
                OffsetList.Add(temp);
            }
        }

        private void onHexEditTextChanged(object sender, EventArgs e)
        {
            HexEditOriginal.Text = _originalFile._BytesAsStringFormatted;
        }
    }
}
