#define VERBOSE_DEBUG
namespace AutoTyper
{
    #region Usings

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;

    #endregion

    /// <summary>
    ///     Code partially inspired by http://blogs.msdn.com/b/toub/archive/2006/05/03/589423.aspx.
    /// </summary>
    internal class AutoTyper : IDisposable
    {
        #region Champs et constantes statiques

        private const int WhKeyboardLl = 13;

        private const int WmKeydown = 0x0100;

        private const int WmKeyup = 0x0101;

        #endregion

        #region Champs

        /// <summary>
        ///     Gets the list the text to be typed for each function key (F1 => AutoTypedText[0] ...)
        /// </summary>
        private readonly string[] autoTypedText = new string[12];

        private readonly IntPtr hookId;

        /// <summary>
        ///     Special keys.
        /// </summary>
        private bool ctrlPressed;

        private int nbOfLettersTyped = 1;

        /// <summary>
        ///     replacing text ?.
        /// </summary>
        private bool replaceText;

        /// <summary>
        ///     Index in the replacement text.
        /// </summary>
        private int replIndex;

        private bool shiftPressed;

        /// <summary>
        ///     Text index in the array AutoTypedText.
        /// </summary>
        private int textIndex;

        /// <summary>
        ///     Bool value used to temporarily desactivate interception (for one keystroke to avoid endless loop).
        /// </summary>
        private bool tmpIntercept = true;

        #endregion

        #region Constructeurs et destructeurs

        /// <summary>
        ///     Initializes a new instance of the <see cref="AutoTyper" /> class.
        /// </summary>
        /// <param name="autoTypedText">List of autotyped text.</param>
        public AutoTyper(IReadOnlyList<string> autoTypedText)
        {
            for (var i = 0; i < Math.Min(autoTypedText.Count, this.autoTypedText.Length); i++)
            {
                this.autoTypedText[i] = autoTypedText[i];
            }

            NativesCalls.LowLevelKeyboardProc proc = this.HookCallback;
            this.hookId = SetHook(proc);
        }

        #endregion

        #region Evènements

        /// <summary>
        ///     Event raised when a key is typed (returns the index key number)
        /// </summary>
        public event EventHandler<int> KeyStroke;

        /// <summary>
        ///     Event raised when the number of letters typed has changed
        /// </summary>
        public event EventHandler<int> NbOfLettersTypedChanged;

        /// <summary>
        ///     Event raised when a scenario is started (returns the scenario number. 0 for the first one)
        /// </summary>
        public event EventHandler<int> Started;

        /// <summary>
        ///     Event raised when a scenario is stopped (no more key or another scenario to start)
        /// </summary>
        public event EventHandler Stopped;

        #endregion

        #region Propriétés et indexeurs

        /// <summary>
        ///     Gets or sets the number of letters typed when striking one key.
        /// </summary>
        public int NbOfLettersTyped
        {
            get => this.nbOfLettersTyped;
            set
            {
                if (this.nbOfLettersTyped == value)
                {
                    return;
                }

                this.nbOfLettersTyped = Math.Min(Math.Max(1, value), 10); // When greater or equal than 11, does not cancel the key stroke
                this.NbOfLettersTypedChanged?.Invoke(this, this.nbOfLettersTyped);
                Debug("Number of letters typed : " + this.nbOfLettersTyped);
            }
        }

        #endregion

        #region Méthodes publiques

        /// <summary>
        ///     Dispose : remove the key hook.
        /// </summary>
        public void Dispose()
        {
            NativesCalls.UnhookWindowsHookEx(this.hookId);
            this.Stopped?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Méthodes privées

        [Conditional("VERBOSE_DEBUG")]
        private static void Debug(string msg)
        {
            System.Diagnostics.Debug.WriteLine(msg);
        }

        private static IntPtr SetHook(NativesCalls.LowLevelKeyboardProc proc)
        {
            using (var curProcess = Process.GetCurrentProcess())
            {
                using (var curModule = curProcess.MainModule)
                {
                    return NativesCalls.SetWindowsHookEx(WhKeyboardLl, proc, NativesCalls.GetModuleHandle(curModule?.ModuleName), 0);
                }
            }
        }

        /// <summary>
        ///     The callback called when a key has been typed.
        /// </summary>
        /// <param name="nCode">Code.</param>
        /// <param name="wParam">wParam.</param>
        /// <param name="lParam">lParam.</param>
        /// <returns>IntPtr.</returns>
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (!this.tmpIntercept || nCode < 0)
            {
                return NativesCalls.CallNextHookEx(this.hookId, nCode, wParam, lParam);
            }

            var vkCode = Marshal.ReadInt32(lParam);
            Debug($"wParam = {wParam}, lParam = {lParam}, vkCode = {vkCode}");

            if (wParam == (IntPtr)WmKeyup)
            {
                switch ((Keys)vkCode)
                {
                    case Keys.LShiftKey:
                    case Keys.RShiftKey:
                        this.shiftPressed = false;
                        Debug("Shift Key released");
                        break;

                    case Keys.LControlKey:
                    case Keys.RControlKey:
                        this.ctrlPressed = false;
                        Debug("Ctrl Key released");
                        break;

                    case Keys.LWin:
                    case Keys.RWin:
                        // _windowsPressed = false;
                        Debug("Windows Key released");
                        break;
                }
            }
            else if (wParam == (IntPtr)WmKeydown)
            {
                switch ((Keys)vkCode)
                {
                    case Keys.LShiftKey:
                    case Keys.RShiftKey:
                        this.shiftPressed = true;
                        Debug("Shift Key pressed");
                        break;

                    case Keys.LControlKey:
                    case Keys.RControlKey:
                        this.ctrlPressed = true;
                        Debug("Ctrl Key pressed");
                        break;

                    case Keys.LWin:
                    case Keys.RWin:
                        // _windowsPressed = true;
                        Debug("Windows Key pressed");
                        break;

                    case Keys.Escape:
                        this.replaceText = false;
                        this.Stopped?.Invoke(this, EventArgs.Empty);
                        Debug("Stop replacing text");
                        break;

                    case Keys.F1:
                    case Keys.F2:
                    case Keys.F3:
                    case Keys.F4:
                    case Keys.F5:
                    case Keys.F6:
                    case Keys.F7:
                    case Keys.F8:
                    case Keys.F9:
                    case Keys.F10:
                    case Keys.F11:
                    case Keys.F12:
                        if (this.ctrlPressed && this.shiftPressed)
                        {
                            this.replaceText = true;
                            this.textIndex = vkCode - 112;
                            this.replIndex = 0;
                            this.tmpIntercept = true;
                            this.Started?.Invoke(this, this.textIndex);
                            Debug("Start replacing text : " + this.autoTypedText[this.textIndex]);
                        }

                        break;

                    default:
                        if (this.ctrlPressed && this.shiftPressed && (Keys)vkCode == Keys.Up)
                        {
                            // Ctrl-Shift-Up increases the number of letters typeds
                            this.NbOfLettersTyped++;
                        }
                        else if (this.ctrlPressed && this.shiftPressed && (Keys)vkCode == Keys.Down)
                        {
                            // Ctrl-Shift-Down decreases the number
                            this.NbOfLettersTyped--;
                        }
                        else if (this.replaceText)
                        {
                            var lettersTyped = false;
                            for (var i = 0; i < this.NbOfLettersTyped; i++)
                            {
                                if (this.replIndex < this.autoTypedText[this.textIndex].Length)
                                {
                                    lettersTyped = true;
                                    this.tmpIntercept = false;
                                    string keys;
                                    switch (this.autoTypedText[this.textIndex][this.replIndex])
                                    {
                                        case '{':
                                        case '}':
                                        case '(':
                                        case ')':
                                        case '%':
                                        case '+':
                                        case '^':
                                        case '~':
                                            keys = $"{{{this.autoTypedText[this.textIndex][this.replIndex]}}}";
                                            break;

                                        default:
                                            keys = this.autoTypedText[this.textIndex][this.replIndex].ToString(CultureInfo.CurrentCulture);
                                            break;
                                    }

                                    Debug($"Replace capture by '{keys}'. Index = {this.replIndex}");
                                    SendKeys.Send(keys);

                                    this.replIndex++;
                                    this.tmpIntercept = true;

                                    this.KeyStroke?.Invoke(this, this.replIndex);
                                }
                                else
                                {
                                    this.replaceText = false;
                                    this.Stopped?.Invoke(this, EventArgs.Empty);
                                    Debug("Stop replacing text : no more input data");
                                }
                            }

                            if (lettersTyped)
                            {
                                // Cancel current keystroke
                                Debug("Return (IntPtr)1");
                                return (IntPtr)1;
                            }
                        }

                        break;
                }
            }

            return NativesCalls.CallNextHookEx(this.hookId, nCode, wParam, lParam);
        }

        #endregion
    }
}
