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
            this.BindingContext = this;
            initTestData();
            setupEncodingPicker();
            initHexEditorBytes();
            setOffsetArea();
            setEncodedStrings();

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

        private void initTestData()
        {
            _Bytes = new byte[512] { //test data with mostly shift-jis encoded text
                0x55, 0x54, 0x46, 0x38, 0x3a, 0xc3, 0xb6, 0xc3, 0xa4, 0xc3, 0xbc, 0xc3, 0x9f, 0x00, 0x61, 0x73, //utf8 test
                0x55, 0x00, 0x31, 0x00, 0x36, 0x00, 0x3a, 0x00, 0xa2, 0x30, 0x00, 0x00, 0xa3, 0x30, 0x00, 0x00, //utf16 LE
                0x00, 0x55, 0x00, 0x31, 0x00, 0x36, 0x00, 0x42, 0x00, 0x45, 0x00, 0x00, 0x04, 0x2e, 0x30, 0xd0, //UTF16 BE
                0xd0, 0x30, 0x00, 0x00, 0xdf, 0x00, 0x00, 0x00, 0x45, 0xf4, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, //UTF32 LE
                0x00, 0x00, 0x30, 0xd0, 0x00, 0x00, 0x00, 0xdf, 0x00, 0x01, 0xf4, 0x45, 0x00, 0x00, 0x00, 0x00, //UTF32 BE
                0x73, 0x5F, 0x25, 0x73, 0x2E, 0x66, 0x6D, 0x69, 0x00, 0x00, 0x00, 0x00, 0x76, 0x65, 0x68, 0x69,
                0x63, 0x6C, 0x65, 0x2F, 0x00, 0x00, 0x00, 0x00, 0x63, 0x61, 0x72, 0x63, 0x6D, 0x6E, 0x74, 0x65,
                0x78, 0x2E, 0x74, 0x70, 0x6C, 0x00, 0x00, 0x00, 0x2E, 0x2E, 0x00, 0x00, 0x76, 0x65, 0x68, 0x69,
                0x63, 0x6C, 0x65, 0x5F, 0x70, 0x61, 0x72, 0x74, 0x73, 0x2F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x43, 0x00, 0x00, 0x00, 0x83, 0x66, 0x83, 0x74,
                0x83, 0x48, 0x83, 0x8B, 0x83, 0x67, 0x8F, 0x87, 0x00, 0x00, 0x00, 0x00, 0x50, 0x00, 0x00, 0x00,
                0x83, 0x7C, 0x83, 0x43, 0x83, 0x93, 0x83, 0x67, 0x8F, 0x87, 0x00, 0x00, 0x52, 0x50, 0x00, 0x00,
                0x83, 0x7C, 0x83, 0x43, 0x83, 0x93, 0x83, 0x67, 0x8B, 0x74, 0x8F, 0x87, 0x00, 0x00, 0x00, 0x00,
                0x90, 0xAB, 0x94, 0x5C, 0x8F, 0x87, 0x00, 0x00, 0x90, 0xAB, 0x94, 0x5C, 0x8B, 0x74, 0x8F, 0x87,
                0x00, 0x00, 0x00, 0x00, 0x57, 0x00, 0x00, 0x00, 0x83, 0x45, 0x83, 0x47, 0x83, 0x43, 0x83, 0x67,
                0x8F, 0x87, 0x00, 0x00, 0x52, 0x57, 0x00, 0x00, 0x83, 0x45, 0x83, 0x47, 0x83, 0x43, 0x83, 0x67,
                0x8B, 0x74, 0x8F, 0x87, 0x00, 0x00, 0x00, 0x00, 0x42, 0x72, 0x61, 0x76, 0x65, 0x20, 0x45, 0x61,
                0x67, 0x6C, 0x65, 0x20, 0x53, 0x74, 0x72, 0x69, 0x6B, 0x65, 0x20, 0x43, 0x6F, 0x6E, 0x64, 0x6F,
                0x72, 0x00, 0x00, 0x00, 0x83, 0x75, 0x83, 0x8C, 0x83, 0x43, 0x83, 0x75, 0x20, 0x83, 0x43, 0x81,
                0x5B, 0x83, 0x4F, 0x83, 0x8B, 0x20, 0x83, 0x58, 0x83, 0x67, 0x83, 0x89, 0x83, 0x43, 0x83, 0x4E,
                0x20, 0x83, 0x52, 0x83, 0x93, 0x83, 0x68, 0x83, 0x8B, 0x00, 0x00, 0x00, 0x47, 0x61, 0x6C, 0x61,
                0x78, 0x79, 0x20, 0x46, 0x61, 0x6C, 0x63, 0x6F, 0x6E, 0x20, 0x43, 0x6F, 0x73, 0x6D, 0x6F, 0x20,
                0x54, 0x65, 0x72, 0x63, 0x65, 0x6C, 0x00, 0x00, 0x83, 0x4D, 0x83, 0x83, 0x83, 0x89, 0x83, 0x4E,
                0x83, 0x56, 0x81, 0x5B, 0x20, 0x83, 0x74, 0x83, 0x40, 0x83, 0x8B, 0x83, 0x52, 0x83, 0x93, 0x20,
                0x83, 0x52, 0x83, 0x58, 0x83, 0x82, 0x20, 0x83, 0x5E, 0x81, 0x5B, 0x83, 0x5A, 0x83, 0x8B, 0x00,
                0x47, 0x69, 0x61, 0x6E, 0x74, 0x20, 0x50, 0x6C, 0x61, 0x6E, 0x65, 0x74, 0x20, 0x43, 0x6F, 0x73,
                0x6D, 0x69, 0x63, 0x20, 0x43, 0x69, 0x72, 0x63, 0x6C, 0x65, 0x00, 0x00, 0x83, 0x57, 0x83, 0x83,
                0x83, 0x43, 0x83, 0x41, 0x83, 0x93, 0x83, 0x67, 0x20, 0x83, 0x76, 0x83, 0x89, 0x83, 0x6C, 0x83,
                0x62, 0x83, 0x67, 0x20, 0x83, 0x52, 0x83, 0x59, 0x83, 0x7E, 0x83, 0x62, 0x83, 0x4E, 0x20, 0x83,
                0x54, 0x81, 0x5B, 0x83, 0x4E, 0x83, 0x8B, 0x00, 0x4D, 0x65, 0x67, 0x61, 0x6C, 0x6F, 0x20, 0x43,
                0x72, 0x75, 0x69, 0x73, 0x65, 0x72, 0x20, 0x48, 0x65, 0x61, 0x76, 0x79, 0x20, 0x42, 0x72, 0x75 };
        }
        
        [DllImport("MorphText.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)] //char* to char*
        private static extern IntPtr ConvertCharStringToCharStringUnsafe(byte[] input, int inputEncoding, int outputEncoding);

        [DllImport("MorphText.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)] // char* to wchar_t*
        private static extern IntPtr ConvertCharStringToWcharStringUnsafe(byte[] input, int inputEncoding, int outputEncoding);

        [DllImport("MorphText.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)] // char* to u32char_t*
        private static extern IntPtr ConvertCharStringToWU32charStringUnsafe(byte[] input, int inputEncoding, int outputEncoding);

        [DllImport("MorphText.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)] // wchar_t* to char*
        private static extern IntPtr ConvertWcharStringToCharStringUnsafe(char[] input, int inputEncoding, int outputEncoding);

        [DllImport("MorphText.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)] // wchar_t* to wchar_t*
        private static extern IntPtr ConvertWcharStringToWcharStringUnsafe(char[] input, int inputEncoding, int outputEncoding);

        [DllImport("MorphText.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)] // wchar_t* to char32_t*
        private static extern IntPtr ConvertWcharStringToU32charStringUnsafe(char[] input, int inputEncoding, int outputEncoding);

        [DllImport("MorphText.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)] // char32_t* to char*
        private static extern IntPtr ConvertU32charStringToCharStringUnsafe(UInt32[] input, int inputEncoding, int outputEncoding);
 
        [DllImport("MorphText.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)] // char32_t* to wchar_t*
        private static extern IntPtr ConvertU32charStringToWcharStringUnsafe(UInt32[] input, int inputEncoding, int outputEncoding);

        [DllImport("MorphText.dll", CallingConvention = CallingConvention.Cdecl)] // char32_t* to char32_t*
        private static extern IntPtr ConvertU32charStringToU32charStringUnsafe(UInt32[] input, int inputEncoding, int outputEncoding);

        [DllImport("MorphText.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void FreeMemoryCharPtr(IntPtr ptr);

        [DllImport("MorphText.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void FreeMemoryWcharPtr(IntPtr ptr);

        [DllImport("MorphText.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void FreeMemoryU32charPtr(IntPtr ptr);

        public string TestConversion()
        {
            byte[] inputBytes = { 0x6F, 0x6F, 0x79, 0x61, 0x6D, 0x61, 0x6E, 0x65, 0x6B, 0x6F, 0x20, 0xB5, 0xB5, 0xD4, 0xCF, 0xC8, 0xBA, 0x81, 0x40, 0x83,
               0x49, 0x83, 0x49, 0x83, 0x84, 0x83, 0x7D, 0x83, 0x6C, 0x83, 0x52, 0x81, 0x40, 0x82, 0xA8, 0x82, 0xA8, 0x82, 0xE2, 0x82,
               0xDC, 0x82, 0xCB, 0x82, 0xB1, 0x81, 0x40, 0x91, 0xE5, 0x8E, 0x52, 0x94, 0x4C, 0x00 };

            // call the ConvertCharStringToWcharStringUnsafe function (char* to wchar_t*)
            IntPtr resultPtr = ConvertCharStringToWcharStringUnsafe(inputBytes, 22, 2);


            // convert the result to a C# string
            string result = Marshal.PtrToStringUni(resultPtr);

            // Free the memory allocated in C++
            FreeMemoryWcharPtr(resultPtr);
            return result;
        }

        public UInt64 _BaseAddress { get; set; }
        public bool _IsBigEndian { get; set; }
        private bool _showAddressesFR = true;
        private bool _keyPressed = false;
        public bool _ShowAddresses
        {
            get => _showAddressesFR;
            set
            {
                _showAddressesFR = value;
                CurrentOffset.IsVisible = value;
                OffsetList.IsVisible = value;
            }
        }
        public byte[] _Bytes { get; set; }
        public string _BytesAsString { get; set; }
        public string _BytesAsStringFormatted { get; set; }
        public UInt64 _PosBytes { get; set; }
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
        }

        public void UpdateOffset()
        {
            _PosBytesStringFormatted = (UInt64)HexEditorBytes.CursorPosition;
            _PosBytes = _PosBytesStringFormatted / 3;
            _PosBytesString = _PosBytes * 2;
            CurrentOffset.Text = (_BaseAddress + _PosBytes).ToString("X");

            if (((_PosBytesStringFormatted+1) % 3 == 0) && _PosBytesStringFormatted != 0)
                ++HexEditorBytes.CursorPosition;
        }
        private void onEncodingPickerIndexChanged(object sender, EventArgs e)
        {
            setEncodedStrings();
        }

        private async Task formatBytesString()
        {
            _BytesAsString = "";
            _BytesAsStringFormatted = "";

            for (int i = 0; i < _Bytes.Length; ++i)
                _BytesAsString += _Bytes[i].ToString("X2");

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

            for (int i = 0; i <= 0x1F0; i += 0x10)
            {
                tempStr += (_BaseAddress + (UInt64)i).ToString("X2");

                if (i < 0x1F0)
                    tempStr += '\n';
            }

            OffsetList.Text = tempStr;
        }

        private async Task setEncodedStrings()
        {
            if (_Bytes == null || _Bytes.Length == 0)
                return;

            Int32 inputEncoding = (Int32)((StringEncodingPickerItem)EncodingPicker.SelectedItem).Id;
            string tempStr = "";
            int count = 32;

            if (_Bytes.Length < 32 * 16)
                count = (32 * 16) % _Bytes.Length;

            switch (inputEncoding)
            {
                case (Int32)StringEncodings.UTF16LE:
                case (Int32)StringEncodings.UTF16BE:
                {
                    for (Int32 i = 0; i <= 0x1F0; i += 0x10)
                    {
                        char[] tempChars = new char[8];

                        for(int c = 0; c < 8; ++c)
                        {
                            tempChars[c] = (char)((Int32)_Bytes[i + c * 2] | ((Int32)_Bytes[i + c * 2 + 1]) << 8);
                        }

                        for (Int32 c = 0; c < 8; ++c)
                        {
                            if (tempChars[c] == 0)
                                if (inputEncoding == (Int32)StringEncodings.UTF16LE)
                                    tempChars[c] = '␀';
                                else
                                    tempChars[c] = (char)0x0024;
                            }

                        IntPtr resultPtr = ConvertWcharStringToWcharStringUnsafe(tempChars, inputEncoding, (Int32)StringEncodings.UTF16LE);
                        StringBuilder tempLine = new StringBuilder(Marshal.PtrToStringUni(resultPtr));
                        FreeMemoryWcharPtr(resultPtr);
                        tempStr += tempLine;

                        if (i < 0x1F0)
                            tempStr += '\n';
                    }
                }
                break;
                case (Int32)StringEncodings.UTF32LE:
                case (Int32)StringEncodings.UTF32BE:
                {
                    for (Int32 i = 0; i <= 0x1F0; i += 0x10)
                    {
                        UInt32[] tempChars = new UInt32[4];

                        for (int c = 0; c < 4; ++c)
                        {
                            tempChars[c] = (UInt32)_Bytes[i + c * 4] | ((UInt32)_Bytes[i + c * 4 + 1] << 8)
                                | ((UInt32)_Bytes[i + c * 4 + 2] << 16) | ((UInt32)_Bytes[i + c * 4 + 3] << 24);
                        }

                        for (Int32 c = 0; c < 4; ++c)
                        {
                            if (tempChars[c] == 0)
                                if(inputEncoding == (Int32)StringEncodings.UTF32LE)
                                    tempChars[c] = '␀';
                                else
                                    tempChars[c] = 0x00240000;
                        }

                        IntPtr resultPtr = ConvertU32charStringToWcharStringUnsafe(tempChars, inputEncoding, (Int32)StringEncodings.UTF16LE);
                        StringBuilder tempLine = new StringBuilder(Marshal.PtrToStringUni(resultPtr));
                        FreeMemoryWcharPtr(resultPtr);
                        tempStr += tempLine;

                        if (i < 0x1F0)
                            tempStr += '\n';
                    }
                }
                break;
                default:
                {
                    for (Int32 i = 0; i <= 0x1F0; i += 0x10)
                    {
                        Byte[] tempBytes = new Byte[16];
                        Array.Copy(_Bytes, i, tempBytes, 0, 16);

                        for (Int32 c = 0; c < 16; ++c)
                        {
                            if(tempBytes[c] == 0)
                                tempBytes[c] = 0x2E;
                        }

                        IntPtr resultPtr = ConvertCharStringToWcharStringUnsafe(tempBytes, inputEncoding, (Int32)StringEncodings.UTF16LE);
                        StringBuilder tempLine = new StringBuilder(Marshal.PtrToStringUni(resultPtr));
                        FreeMemoryWcharPtr(resultPtr);
                        tempStr += tempLine;

                        if (i < 0x1F0)
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
            if (!_isInitialized)
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
                    e.Handler = true;
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
            }
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
                //editor.TextChanged -= onHexEditTextChanged;
                editor.Text = text;
                //editor.TextChanged += onHexEditTextChanged;
                tcs.SetResult(true); // Signal that the UI update is complete
            });

            await tcs.Task; // Wait for the UI update to complete
            //_isInitialized = true;

            _isTextChanging = false;
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
