using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.Maui.Platform;
using Microsoft.Maui.ApplicationModel;
using System.Runtime.InteropServices;
using Microsoft.Maui.Controls;
using System.Security.Cryptography;
using Microsoft.UI.Xaml.Input;
using Microsoft.Maui.Controls;
using System.Runtime.CompilerServices;

namespace HexEditor
{
    public class HexEditorBehavior : Behavior<Editor>
    {
        protected override void OnAttachedTo(Editor editor)
        {
            base.OnAttachedTo(editor);
            editor.HandlerChanged += OnHandlerChanged;
           // editor.TextChanged += OnTextChanged;
        }

        protected override void OnDetachingFrom(Editor editor)
        {
            base.OnDetachingFrom(editor);
            editor.HandlerChanged -= OnHandlerChanged;
            //editor.TextChanged -= OnTextChanged;
        }

        private void OnHandlerChanged(object sender, System.EventArgs e)
        {
            if (sender is Editor editor && editor.Handler != null)
            {
                var platformView = editor.Handler.PlatformView as TextBox;

                if (platformView != null)
                {
                    platformView.Paste -= OnPaste;
                    platformView.Paste += OnPaste;
                    platformView.KeyDown -= OnKeyDown;
                    platformView.KeyDown += OnKeyDown;
                    platformView.KeyUp -= OnKeyUp;
                    platformView.KeyUp += OnKeyUp;
                    platformView.PointerReleased -= OnPointerReleased;
                    platformView.PointerReleased += OnPointerReleased;
                }
            }
        }

        private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            var textBox = sender as TextBox;

            if (textBox != null)
            {
                var editor = textBox.DataContext as Editor; //is always null. idk how to fix this

                if (editor != null)
                {
                    editor.SelectionLength = 0;
                }
            }
        }

        private void OnPaste(object sender, TextControlPasteEventArgs e)
        {
            // Block the paste action
            e.Handled = true;
        }

        private void OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            // Block backspace and delete keys
            if (e.Key == Windows.System.VirtualKey.Back || e.Key == Windows.System.VirtualKey.Delete)
            {
                e.Handled = true;
            }

            //does not recognize arrow keys and delete key
        }

        private void OnKeyUp(object sender, KeyRoutedEventArgs e)
        {
            //does recognize arrow keys and delete key

            if (e.Key == Windows.System.VirtualKey.Delete) //useless here because text has already been modified. Accessing the parent Editor always results in a null object 
            {
                e.Handled = true;
            }

            // Handle arrow keys
            if (e.Key == Windows.System.VirtualKey.Left || e.Key == Windows.System.VirtualKey.Right
             || e.Key == Windows.System.VirtualKey.Up || e.Key == Windows.System.VirtualKey.Down)
            {
                var textBox = sender as TextBox;

                if (textBox != null)
                {
                    var editor = textBox.DataContext as Editor; //is always null. idk how to fix this
                    var hexEditor = editor?.Parent?.Parent as HexEditor;

                    if (hexEditor != null)
                        hexEditor.UpdateOffset();
                }
            }
        }

       // private void OnTextChanged(object sender, Microsoft.Maui.Controls.TextChangedEventArgs e)
       // {

       // }
    }

    enum StringEncodings : Int32
    {
        UTF8 = 1,
        UTF16LE,
        UTF16BE,
        UTF32LE,
        UTF32BE,
        ASCII,
        ISO_8859_1,
        ISO_8859_2,
        ISO_8859_3,
        ISO_8859_4,
        ISO_8859_5,
        ISO_8859_6,
        ISO_8859_7,
        ISO_8859_8,
        ISO_8859_9,
        ISO_8859_10,
        ISO_8859_11,
        ISO_8859_13,
        ISO_8859_14,
        ISO_8859_15,
        ISO_8859_16,
        SHIFTJIS_CP932,
        JIS_X_0201_FULLWIDTH,
        JIS_X_0201_HALFWIDTH,
        KS_X_1001,
        Reserved,
        POKEMON_GEN1_ENGLISH,
        POKEMON_GEN1_FRENCH_GERMAN,
        POKEMON_GEN1_ITALIAN_SPANISH,
        POKEMON_GEN1_JAPANESE,
        POKEMON_GEN2_ENGLISH
    };

    public class StringEncodingPickerItem
    {
        public Int32 Id { get; set; }
        public string DisplayName { get; set; }
    }

    public partial class HexEditor : ContentView
    {
        public HexEditor()
        {
            InitializeComponent();
            setupEncodingPicker();
            generateDataViewText();

            Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("CurrentOffset", (handler, view) =>
            {
                if (view is Entry)
                    handler.PlatformView.Padding = new Microsoft.UI.Xaml.Thickness(0, 0, 0, 0);
            });

            Microsoft.Maui.Handlers.PickerHandler.Mapper.AppendToMapping("EncodingPicker", (handler, view) =>
            {
                if (view is Picker)
                    handler.PlatformView.Padding = new Microsoft.UI.Xaml.Thickness(0, 0, 0, 0);
            });
        }
        
        [DllImport("MorphText.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)] //char* to char*
        public static extern IntPtr ConvertCharStringToCharStringUnsafe(byte[] input, int inputEncoding, int outputEncoding);

        [DllImport("MorphText.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)] // char* to wchar_t*
        public static extern IntPtr ConvertCharStringToWcharStringUnsafe(byte[] input, int inputEncoding, int outputEncoding);

        [DllImport("MorphText.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)] // char* to u32char_t*
        public static extern IntPtr ConvertCharStringToWU32charStringUnsafe(byte[] input, int inputEncoding, int outputEncoding);

        [DllImport("MorphText.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)] // wchar_t* to char*
        public static extern IntPtr ConvertWcharStringToCharStringUnsafe(char[] input, int inputEncoding, int outputEncoding);

        [DllImport("MorphText.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)] // wchar_t* to wchar_t*
        public static extern IntPtr ConvertWcharStringToWcharStringUnsafe(char[] input, int inputEncoding, int outputEncoding);

        [DllImport("MorphText.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)] // wchar_t* to char32_t*
        public static extern IntPtr ConvertWcharStringToU32charStringUnsafe(char[] input, int inputEncoding, int outputEncoding);

        [DllImport("MorphText.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)] // char32_t* to char*
        public static extern IntPtr ConvertU32charStringToCharStringUnsafe(UInt32[] input, int inputEncoding, int outputEncoding);
 
        [DllImport("MorphText.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)] // char32_t* to wchar_t*
        public static extern IntPtr ConvertU32charStringToWcharStringUnsafe(UInt32[] input, int inputEncoding, int outputEncoding);

        [DllImport("MorphText.dll", CallingConvention = CallingConvention.Cdecl)] // char32_t* to char32_t*
        public static extern IntPtr ConvertU32charStringToU32charStringUnsafe(UInt32[] input, int inputEncoding, int outputEncoding);

        [DllImport("MorphText.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void FreeMemoryCharPtr(IntPtr ptr);

        [DllImport("MorphText.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void FreeMemoryWcharPtr(IntPtr ptr);

        [DllImport("MorphText.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void FreeMemoryU32charPtr(IntPtr ptr);

        //public bool _IsReadOnly { get; set; }
        private UInt64 _baseAddressFR = 0;
        public UInt64 _BaseAddress
        {
            get => _baseAddressFR;
            set 
            {
                _baseAddressFR = value & 0xFFFFFFFFFFFFFFF0;

                if(_Bytes == null)
                    return;

                if (_CurrentOffset < _baseAddressFR || _CurrentOffset >= _baseAddressFR + _currentOffsetFR)
                {
                    SetCurrentOffset(_baseAddressFR);
                }
            }
        }
        private UInt64 _fileOffset = 0;
        private UInt64 _currentOffsetFR = 0;
        private UInt64 _fileOffsetDirty = 0;
        public HexEditor SyncTarget { get; set; }
        public UInt64 _CurrentOffset
        {
            get => _currentOffsetFR;
            set
            {
                if (CurrentOffset == null)
                    return;

                if (_Bytes == null)
                    return;

                _fileOffsetDirty = value - _BaseAddress;
                _currentOffsetFR = value & 0xFFFFFFFFFFFFFFF0;
                _fileOffset = value - _BaseAddress;
                _viewPosition = _fileOffset;

                if (CurrentOffset == null)
                    return;

                CurrentOffset.Text = _currentOffsetFR.ToString("X");
            }
        }
        public bool _IsBigEndian { get; set; }
        private bool _showAddressesFR = true;
        private double _fontSizeFR = 12;
        public double _FontSize
        {
            get => _fontSizeFR;
            set
            {
                _fontSizeFR = value;
                CurrentOffset.FontSize = _fontSizeFR;
                OffsetList.FontSize = _fontSizeFR;
                BytesHeader.FontSize = _fontSizeFR;
                HexEditorBytes.FontSize = _fontSizeFR;
                EncodingPicker.FontSize = _fontSizeFR;
                EncodedStrings.FontSize = _fontSizeFR;
            }
        }
        public bool _ShowAddressArea
        {
            get => _showAddressesFR;
            set
            {
                _showAddressesFR = value;

                if(CurrentOffset != null)
                    CurrentOffset.IsVisible = value;

                if (OffsetList != null)
                    OffsetList.IsVisible = value;

                if (ParentGrid == null)
                    return;

                ParentGrid.ColumnDefinitions[0] = new Microsoft.Maui.Controls.ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) };
            }
        }
        private bool _showTextFR = true;
        public bool _ShowTextArea
        {
            get => _showTextFR;
            set
            {
                _showTextFR = value;


                if (EncodingPicker != null)
                    EncodingPicker.IsVisible = value;

                if (EncodedStrings != null)
                    EncodedStrings.IsVisible = value;

                if (ParentGrid == null)
                    return;

                ParentGrid.ColumnDefinitions[2] = new Microsoft.Maui.Controls.ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) };
            }
        }
        private Int32 _currentCharacterEncodingFR = 1;
        public Int32 _CurrentCharacterEncoding
        { 
            get => _currentCharacterEncodingFR;
            set
            {
                _currentCharacterEncodingFR = value;

                for (Int32 i = 0; i < _StringEncodingPairs.Count; ++i)
                    if (_StringEncodingPairs[i].Id == value)
                        EncodingPicker.SelectedIndex = i;
            }
        }
        public byte[] _Bytes { get; set; }
        public string _BytesAsString { get; set; }
        public string _BytesAsStringFormatted { get; set; }
        private UInt64 _viewPosition = 0;
        public UInt64 _PosBytesRelative { get; set; } //relative to _fileOffset
        public UInt64 _PosBytesString { get; set; }
        public UInt64 _PosBytesStringFormatted { get; set; }
        public UInt64 _SelectedPosBytes { get; set; }
        public UInt64 _SelectedPosBytesString { get; set; }
        public UInt64 _SelectedPosBytesStringFormatted { get; set; }
        public List<StringEncodingPickerItem> _StringEncodingPairs { get; private set; }
        private bool _isInitialized = false;

        private async Task initHexEditorBytes()
        {
            await formatBytesString();
            await setEditorTextAsync(HexEditorBytes, _BytesAsStringFormatted);
            _isInitialized = true;
        }

        private void onEditorTapped(object sender, EventArgs e)
        {
            UpdateOffset();
            generateDataViewText();
        }

        public void UpdateOffset()
        {
            _PosBytesStringFormatted = (UInt64)HexEditorBytes.CursorPosition;
            _PosBytesRelative = _PosBytesStringFormatted / 3;
            //_PosBytesString = _PosBytes * 2;
            CurrentOffset.Text = (_BaseAddress + _PosBytesRelative + _fileOffset).ToString("X");

            if (((_PosBytesStringFormatted+1) % 3 == 0) && _PosBytesStringFormatted != 0)
                ++HexEditorBytes.CursorPosition;

            _fileOffsetDirty = _PosBytesRelative + _fileOffset;
        }

        private void onEncodingPickerIndexChanged(object sender, EventArgs e)
        {
            setEncodedStrings();
        }

        private void onEnterPressed(object sender, EventArgs e)
        {
            if (_Bytes == null)
                return;

            _CurrentOffset = Convert.ToUInt64(CurrentOffset.Text, 16);
            formatBytesString();
            setEditorTextAsync(HexEditorBytes, _BytesAsStringFormatted);
            setOffsetArea();
            setEncodedStrings();
            CurrentOffset.Text = _CurrentOffset.ToString("X");

            if (SyncTarget == null)
                return;

            SyncTarget.SetCurrentOffset(_CurrentOffset);
        }

        private UInt32 getNextBytesAmount()
        {
            _fileOffset = _currentOffsetFR - _baseAddressFR;
            UInt32 bytesAmount = 0x200;

            if (_Bytes.Length < bytesAmount)
                bytesAmount = (UInt32)_Bytes.Length;

            if (bytesAmount > (UInt32)_Bytes.Length - _fileOffset)
                bytesAmount = (UInt32)_Bytes.Length - (UInt32)_fileOffset;

            return bytesAmount;
        }

        private async Task formatBytesString()
        {
            _BytesAsString = "";
            _BytesAsStringFormatted = "";
            UInt32 bytesAmount = getNextBytesAmount();

            for (UInt64 i = 0; i < bytesAmount; ++i)
                _BytesAsString += _Bytes[_fileOffset + i].ToString("X2");

            for (int i = 0; i < _BytesAsString.Length; i += 2)
            {
                if ((i & 0x1F) == 0x1E && i > 0)
                    _BytesAsStringFormatted += _BytesAsString.Substring(i, 2) + "\n";
                else if (i == _BytesAsString.Length - 1)
                    _BytesAsStringFormatted += _BytesAsString.Substring(i, 2);
                else
                    _BytesAsStringFormatted += _BytesAsString.Substring(i, 2) + " ";
            }
        }

        private void setOffsetArea()
        {
            string tempStr = "";
            UInt32 bytesAmount = getNextBytesAmount();

            if (bytesAmount >= 0x1F1)
                bytesAmount = 0x1F0;
            else
            {
                if ((bytesAmount & 0xF) == 0)
                    bytesAmount -= 0x10;
                else
                    bytesAmount = bytesAmount & 0xFFFFFFF0;
            }

            for (int i = 0; i <= bytesAmount; i += 0x10)
            {
                tempStr += (_CurrentOffset + (UInt64)i).ToString("X2");

                if (i < bytesAmount)
                    tempStr += '\n';
            }

            OffsetList.Text = tempStr;
        }

        private async Task setEncodedStrings()
        {
            if (_Bytes == null || _Bytes.Length == 0)
                return;

            UInt32 bytesAmount = getNextBytesAmount();

            if (bytesAmount >= 0x1F1)
                bytesAmount = 0x1F0;
            else
            {
                if ((bytesAmount & 0xF) == 0)
                    bytesAmount -= 0x10;
                else
                    bytesAmount = bytesAmount & 0xFFFFFFF0;
            }

            Int32 inputEncoding = (Int32)((StringEncodingPickerItem)EncodingPicker.SelectedItem).Id;
            string tempStr = "";

            switch (inputEncoding)
            {
                case (Int32)StringEncodings.UTF16LE:
                case (Int32)StringEncodings.UTF16BE:
                {
                    for (UInt64 i = 0; i <= bytesAmount; i += 0x10)
                    {
                        char[] tempChars = new char[8];

                        for(UInt64 c = 0; c < 8; ++c)
                        {
                            tempChars[c] = (char)((Int32)_Bytes[_fileOffset + i + c * 2] | ((Int32)_Bytes[_fileOffset + i + c * 2 + 1]) << 8);
                        }

                        for (Int32 c = 0; c < 8; ++c)
                        {
                            switch ((Int32)tempChars[c])
                            {
                                case 0:
                                    if (inputEncoding == (Int32)StringEncodings.UTF32LE)
                                        tempChars[c] = '␀';
                                    else
                                        tempChars[c] = (char)0x0024;
                                break;
                                case 0x0A:
                                    if (inputEncoding == (Int32)StringEncodings.UTF32LE)
                                        tempChars[c] = '␤';
                                    else
                                        tempChars[c] = (char)0x2424;
                                break;
                                case 0x09:
                                    if (inputEncoding == (Int32)StringEncodings.UTF32LE)
                                        tempChars[c] = '␉';
                                    else
                                        tempChars[c] = (char)0x0924;
                                break;
                                case 0x0B:
                                    if (inputEncoding == (Int32)StringEncodings.UTF32LE)
                                        tempChars[c] = '␋';
                                    else
                                        tempChars[c] = (char)0x0B24;
                                break;
                                case 0x0C:
                                    if (inputEncoding == (Int32)StringEncodings.UTF32LE)
                                        tempChars[c] = '␌';
                                    else
                                        tempChars[c] = (char)0x0C24;
                                break;
                                case 0x0D:
                                    if (inputEncoding == (Int32)StringEncodings.UTF32LE)
                                        tempChars[c] = '␍';
                                    else
                                        tempChars[c] = (char)0x0D24;
                                break;
                            }
                        }

                        IntPtr resultPtr = ConvertWcharStringToWcharStringUnsafe(tempChars, inputEncoding, (Int32)StringEncodings.UTF16LE);
                        StringBuilder tempLine = new StringBuilder(Marshal.PtrToStringUni(resultPtr));
                        FreeMemoryWcharPtr(resultPtr);
                        tempStr += tempLine;

                        if (i < bytesAmount)
                            tempStr += '\n';
                    }
                }
                break;
                case (Int32)StringEncodings.UTF32LE:
                case (Int32)StringEncodings.UTF32BE:
                {
                    for (UInt64 i = 0; i <= bytesAmount; i += 0x10)
                    {
                        UInt32[] tempChars = new UInt32[4];

                        for (UInt64 c = 0; c < 4; ++c)
                        {
                            tempChars[c] = (UInt32)_Bytes[_fileOffset + i + c * 4] | ((UInt32)_Bytes[_fileOffset + i + c * 4 + 1] << 8)
                                | ((UInt32)_Bytes[_fileOffset + i + c * 4 + 2] << 16) | ((UInt32)_Bytes[_fileOffset + i + c * 4 + 3] << 24);
                        }

                        for (Int32 c = 0; c < 4; ++c)
                        {
                            switch(tempChars[c])
                            {
                                case 0:
                                    if (inputEncoding == (Int32)StringEncodings.UTF32LE)
                                        tempChars[c] = '␀';
                                    else
                                        tempChars[c] = 0x00240000;
                                break;
                                case 0x0A:
                                    if (inputEncoding == (Int32)StringEncodings.UTF32LE)
                                        tempChars[c] = '␤';
                                    else
                                        tempChars[c] = 0x24240000;
                                break;
                                case 0x09:
                                    if (inputEncoding == (Int32)StringEncodings.UTF32LE)
                                        tempChars[c] = '␉';
                                    else
                                        tempChars[c] = 0x09240000;
                                break;
                                case 0x0B:
                                    if (inputEncoding == (Int32)StringEncodings.UTF32LE)
                                        tempChars[c] = '␋';
                                    else
                                        tempChars[c] = 0x0B240000;
                                break;
                                case 0x0C:
                                    if (inputEncoding == (Int32)StringEncodings.UTF32LE)
                                        tempChars[c] = '␌';
                                    else
                                        tempChars[c] = 0x0C240000;
                                break;
                                case 0x0D:
                                    if (inputEncoding == (Int32)StringEncodings.UTF32LE)
                                        tempChars[c] = '␍';
                                    else
                                        tempChars[c] = 0x0D240000;
                                break;
                            }  
                        }

                        IntPtr resultPtr = ConvertU32charStringToWcharStringUnsafe(tempChars, inputEncoding, (Int32)StringEncodings.UTF16LE);
                        StringBuilder tempLine = new StringBuilder(Marshal.PtrToStringUni(resultPtr));
                        FreeMemoryWcharPtr(resultPtr);
                        tempStr += tempLine;

                        if (i < bytesAmount)
                            tempStr += '\n';
                    }
                }
                break;
                default:
                {
                    for (Int32 i = 0; i <= bytesAmount; i += 0x10)
                    {
                        Byte[] tempBytes = new Byte[16];
                        Array.Copy(_Bytes, (Int32)_fileOffset + i, tempBytes, 0, 16);

                        for (Int32 c = 0; c < 16; ++c)
                        {
                            if(tempBytes[c] == 0 || tempBytes[c] == 0x0D)
                                tempBytes[c] = 0x2E;
                            else if((tempBytes[c] == 0x09 || tempBytes[c] == 0x0A || tempBytes[c] == 0x0B || tempBytes[c] == 0x0C) 
                                    && inputEncoding < (Int32)StringEncodings.POKEMON_GEN1_ENGLISH)
                                        tempBytes[c] = 0x20;
                        }

                        IntPtr resultPtr = ConvertCharStringToWcharStringUnsafe(tempBytes, inputEncoding, (Int32)StringEncodings.UTF16LE);
                        StringBuilder tempLine = new StringBuilder(Marshal.PtrToStringUni(resultPtr));
                        FreeMemoryWcharPtr(resultPtr);
                        tempStr += tempLine;

                        if (i < bytesAmount)
                            tempStr += '\n';
                        }
                }
                break;
            }

            EncodedStrings.Text = tempStr;
        }

        private bool _isTextChanging = false;
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        private async void onHexEditTextChanged(object sender, Microsoft.Maui.Controls.TextChangedEventArgs e)
        {
            /*if (!_isInitialized)
                return;

            await _semaphore.WaitAsync();

            try
            {
                if (_isTextChanging)
                    return;

                _isTextChanging = true;
                var editor = sender as Editor;
                Int32 pos = editor.CursorPosition;

                if (pos % 3 == 0 && pos != 0) //reset if char entered at an illegal position
                {
                    //e.Handler = true;
                    await setEditorTextAsync(editor, e.OldTextValue);
                    _isTextChanging = false;
                    return;
                }

                if(editor.Text.Length < 1536) //reset if selected text has changed
                {
                    await setEditorTextAsync(editor, e.OldTextValue);
                    _isTextChanging = false;
                    return;
                }

                if(pos == 0) //no edit has been made
                {
                    _isTextChanging = false;
                    return;
                }

                char c = editor.Text[pos-1];

                if (!((c >= '0' && c <= '9')
                    || (c >= 'A' && c <= 'F')
                    || (c >= 'a' && c <= 'f')))
                {
                    await setEditorTextAsync(editor, e.OldTextValue);
                    _isTextChanging = false;
                    return;
                }

                if((c >= 'a' && c <= 'f'))
                    c = (char)(c - 0x20);

                await setByte(editor.CursorPosition, c);
                await initHexEditorBytes();
                await setEncodedStrings();
                _isTextChanging = false;
            }
            finally
            {
                _semaphore.Release();
            }*/
        }

        private async Task setByte(Int32 cursorPos, char nibble)
        {
            UInt64 fileOffset = (UInt64)cursorPos / 3;
            Byte ogByte = _Bytes[fileOffset];
            Byte tempByte = ogByte;

            if((cursorPos-1) % 3 == 0)
            {
                tempByte &= 0x0F;
                tempByte |= (Byte)(Convert.ToByte(nibble.ToString(), 16) << 4);
                ++HexEditorBytes.CursorPosition;
            }
            else
            {
                tempByte &= 0xF0;
                tempByte |= (Byte)Convert.ToByte(nibble.ToString(), 16);
                HexEditorBytes.CursorPosition += 2;
            }

            _Bytes[fileOffset] = tempByte;
        }

        private async Task setEditorTextAsync(Editor editor, string text)
        {
            _isTextChanging = true;
            //_isInitialized = false;
            var tcs = new TaskCompletionSource<bool>();

            // Use the Dispatcher to ensure the UI update happens on the main thread
            //BindableObject.Dispatcher.Dispatch
            Device.BeginInvokeOnMainThread(() =>
            {
                editor.TextChanged -= onHexEditTextChanged;
                editor.Text = text;
                tcs.SetResult(true); // Signal that the UI update is complete
                editor.TextChanged += onHexEditTextChanged;
            });

            await tcs.Task; // Wait for the UI update to complete
            //_isInitialized = true;

            _isTextChanging = false;
        }

        public void SetBinaryData(byte[] data)
        {
            _Bytes = data;
            initHexEditorBytes();
            setOffsetArea();
            setEncodedStrings();
            generateDataViewText();
        }

        public void SetCurrentOffset(UInt64 offset)
        {
            _CurrentOffset = offset;
            initHexEditorBytes();
            setOffsetArea();
            setEncodedStrings();
            generateDataViewText();
        }

        public void SetBaseAddress(UInt64 baseAddress)
        {
            _BaseAddress = baseAddress;
            setOffsetArea();
        }

        private void generateDataViewText()
        {
            if(_Bytes == null)
                return;

            string dataView = "";
            dataView += "Bool: " + getValueAsString<bool>() + "\n";
            dataView += "Int8: " + getValueAsString<SByte>() + "\n";
            dataView += "UInt8: " + getValueAsString<Byte>() + "\n";
            dataView += "Int16: " + getValueAsString<Int16>() + "\n";
            dataView += "UInt16: " + getValueAsString<UInt16>() + "\n";
            dataView += "Int32: " + getValueAsString<Int32>() + "\n";
            dataView += "UInt32: " + getValueAsString<UInt32>() + "\n";
            dataView += "Int64: " + getValueAsString<Int64>() + "\n";
            dataView += "UInt64: " + getValueAsString<UInt64>() + "\n";
            dataView += "Single: " + getValueAsString<float>() + "\n";
            dataView += "Double: " + getValueAsString<double>();
            DataView.Text = dataView;
        }

        private static T byteSwap<T>(T value) where T : unmanaged
        {
            Span<byte> bytes = stackalloc byte[Unsafe.SizeOf<T>()];
            MemoryMarshal.Write(bytes, ref value);
            bytes.Reverse();
            return MemoryMarshal.Read<T>(bytes);
        }

        private T getValueFromBytes<T>() where T : unmanaged
        {
            if (_Bytes == null)
                throw new ArgumentNullException(nameof(_Bytes));

            if ((_fileOffsetDirty + (UInt64)Unsafe.SizeOf<T>()) > (UInt64)_Bytes.Length)
                throw new ArgumentOutOfRangeException(nameof(_fileOffsetDirty));

            Span<byte> span = new Span<byte>(_Bytes, (int)_fileOffsetDirty, Unsafe.SizeOf<T>());
            Span<T> typedSpan = MemoryMarshal.Cast<byte, T>(span);
            return typedSpan[0];
        }

        private string getValueAsString<T>() where T : unmanaged
        {
            T value = getValueFromBytes<T>();

            if(_IsBigEndian && Unsafe.SizeOf<T>() > 1)
                value = byteSwap(value);

            string output = value.ToString() + " | 0x";

            if(isIntegralType<T>() != 0)
            { 
                switch (isIntegralType<T>())
                {
                    case 1:
                        if (typeof(T) == typeof(SByte))
                            output += ((SByte)(object)value).ToString("X2");
                        else
                            output += Convert.ToByte(value).ToString("X2");
                        break;
                    case 2:
                        if (typeof(T) == typeof(Int16))
                            output += ((Int16)(object)value).ToString("X4");
                        else
                            output += Convert.ToUInt16(value).ToString("X4");
                        break;
                    case 4:
                        if (typeof(T) == typeof(Int32))
                            output += ((Int32)(object)value).ToString("X8");
                        else
                            output += Convert.ToUInt32(value).ToString("X8");
                        break;
                    case 8:
                        if (typeof(T) == typeof(Int64))
                            output += ((Int64)(object)value).ToString("X16");
                        else
                            output += Convert.ToUInt64(value).ToString("X16");
                        break;
                }
            }

            if (isFloatType<T>() != 0)
            {
                switch (isFloatType<T>())
                {
                    case 4:
                        output += BitConverter.SingleToInt32Bits(Convert.ToSingle(value)).ToString("X");
                    break;
                    case 8:
                        output += BitConverter.DoubleToInt64Bits(Convert.ToDouble(value)).ToString("X");
                    break;
                    //case 16:
                    //    output += BitConverter.ToInt128(value).ToString("X");
                    //break;
                }
            }

            return output;
        }

        private static UInt32 isIntegralType<T>() where T : unmanaged
        {
            Type type = typeof(T);
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.Boolean:
                    return 1;
                case TypeCode.Int16:
                case TypeCode.UInt16:
                    return 2;
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Char:
                    return 4;
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    return 8;
                default:
                    return 0;
            }
        }

        private static UInt32 isFloatType<T>() where T : unmanaged
        {
            Type type = typeof(T);
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Single:
                    return 4;
                case TypeCode.Double:
                    return 8;
                case TypeCode.Decimal:
                    return 16;
                default:
                    return 0;
            }
        }

        public void SetBytes(byte[] data, UInt64 offset)
        {
            if(data == null)
                return;

            UInt64 writeOffset = offset - _BaseAddress;

            if ((UInt64)data.Length + writeOffset > (UInt64)_Bytes.Length)
                return;

            Array.Copy(data, 0, _Bytes, (Int32)writeOffset, data.Length);

            if(offset + (UInt64)data.Length >= _CurrentOffset 
                && offset < _CurrentOffset + ((UInt64)_Bytes.Length < 0x200 ? (UInt64)_Bytes.Length : 0x200))
            {
                initHexEditorBytes();
                setEncodedStrings();
            }
        }

        public byte[] GetBytes(UInt64 offset, Int32 count)
        {
            byte[] bytes = new byte[count];
            UInt64 readOffset = offset - _BaseAddress;

            if (readOffset + (UInt64)count >= (UInt64)_Bytes.Length)
                return null;

            Array.Copy(_Bytes, (Int32)readOffset, bytes, 0, count);
            return bytes;
        }

        private void setupEncodingPicker()
        {
            _StringEncodingPairs = new List<StringEncodingPickerItem>
            {
                new StringEncodingPickerItem { Id = (Int32)StringEncodings.UTF8, DisplayName = "UTF-8" },
                new StringEncodingPickerItem { Id = (Int32)StringEncodings.UTF16LE, DisplayName = "UTF-16 LE" },
                new StringEncodingPickerItem { Id = (Int32)StringEncodings.UTF16BE, DisplayName = "UTF-16 BE" },
                new StringEncodingPickerItem { Id = (Int32)StringEncodings.UTF32LE, DisplayName = "UTF-32 LE" },
                new StringEncodingPickerItem { Id = (Int32)StringEncodings.UTF32BE, DisplayName = "UTF-32 BE" },
                new StringEncodingPickerItem { Id = (Int32)StringEncodings.ASCII, DisplayName = "ASCII" },
                new StringEncodingPickerItem { Id = (Int32)StringEncodings.ISO_8859_1, DisplayName = "ISO-8859-1" },
                new StringEncodingPickerItem { Id = (Int32)StringEncodings.ISO_8859_2, DisplayName = "ISO-8859-2" },
                new StringEncodingPickerItem { Id = (Int32)StringEncodings.ISO_8859_3, DisplayName = "ISO-8859-3" },
                new StringEncodingPickerItem { Id = (Int32)StringEncodings.ISO_8859_4, DisplayName = "ISO-8859-4" },
                new StringEncodingPickerItem { Id = (Int32)StringEncodings.ISO_8859_5, DisplayName = "ISO-8859-5" },
                new StringEncodingPickerItem { Id = (Int32)StringEncodings.ISO_8859_6, DisplayName = "ISO-8859-6" },
                new StringEncodingPickerItem { Id = (Int32)StringEncodings.ISO_8859_7, DisplayName = "ISO-8859-7" },
                new StringEncodingPickerItem { Id = (Int32)StringEncodings.ISO_8859_8, DisplayName = "ISO-8859-8" },
                new StringEncodingPickerItem { Id = (Int32)StringEncodings.ISO_8859_9, DisplayName = "ISO-8859-9" },
                new StringEncodingPickerItem { Id = (Int32)StringEncodings.ISO_8859_10, DisplayName = "ISO-8859-10" },
                new StringEncodingPickerItem { Id = (Int32)StringEncodings.ISO_8859_11, DisplayName = "ISO-8859-11" },
                new StringEncodingPickerItem { Id = (Int32)StringEncodings.ISO_8859_13, DisplayName = "ISO-8859-13" },
                new StringEncodingPickerItem { Id = (Int32)StringEncodings.ISO_8859_14, DisplayName = "ISO-8859-14" },
                new StringEncodingPickerItem { Id = (Int32)StringEncodings.ISO_8859_15, DisplayName = "ISO-8859-15" },
                new StringEncodingPickerItem { Id = (Int32)StringEncodings.ISO_8859_16, DisplayName = "ISO-8859-16" },
                new StringEncodingPickerItem { Id = (Int32)StringEncodings.SHIFTJIS_CP932, DisplayName = "Shift-JIS CP932" },
                new StringEncodingPickerItem { Id = (Int32)StringEncodings.JIS_X_0201_FULLWIDTH, DisplayName = "JIS X 0201 Fullwidth" },
                new StringEncodingPickerItem { Id = (Int32)StringEncodings.JIS_X_0201_HALFWIDTH, DisplayName = "JIS X 0201 Halfwidth" },
                new StringEncodingPickerItem { Id = (Int32)StringEncodings.KS_X_1001, DisplayName = "KS X 1001" },
                new StringEncodingPickerItem { Id = (Int32)StringEncodings.POKEMON_GEN1_ENGLISH, DisplayName = "Pokémon Gen 1 English" },
                new StringEncodingPickerItem { Id = (Int32)StringEncodings.POKEMON_GEN1_FRENCH_GERMAN, DisplayName = "Pokémon Gen 1 French German" },
                new StringEncodingPickerItem { Id = (Int32)StringEncodings.POKEMON_GEN1_ITALIAN_SPANISH, DisplayName = "Pokémon Gen 1 Italian Spanish" },
                new StringEncodingPickerItem { Id = (Int32)StringEncodings.POKEMON_GEN1_JAPANESE, DisplayName = "Pokémon Gen 1 Japanese" },
                new StringEncodingPickerItem { Id = (Int32)StringEncodings.POKEMON_GEN2_ENGLISH, DisplayName = "Pokémon Gen 2 English" }
            };

            EncodingPicker.ItemsSource = _StringEncodingPairs;
            EncodingPicker.ItemDisplayBinding = new Binding("DisplayName");
            EncodingPicker.SelectedIndex = 0;
        }
    }
}