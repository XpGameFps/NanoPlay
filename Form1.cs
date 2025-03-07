using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Globalization;
using System.Linq;

namespace NanoPlay
{
    public partial class Form1 : Form
    {
        // Importação da API do Windows
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private static readonly IntPtr HWND_TOP = IntPtr.Zero;
        private const uint SWP_SHOWWINDOW = 0x0040;

        private List<(IntPtr Handle, string Title)> windows = new List<(IntPtr, string)>();
        private ComboBox windowList;
        private TextBox widthBox;
        private TextBox heightBox;
        private Label widthLabel;
        private Label heightLabel;
        private Button resizeButton;
        private Button refreshButton;
        private CheckBox themeToggle;
        private ComboBox languageSelector;
        private Button donationButton;

        // Dicionário de traduções
        private Dictionary<string, Dictionary<string, string>> translations = new Dictionary<string, Dictionary<string, string>>
        {
            {
                "English", new Dictionary<string, string>
                {
                    {"Title", "NanoPlay"},
                    {"Width", "Width:"},
                    {"Height", "Height:"},
                    {"Resize", "Resize"},
                    {"Refresh", "Refresh"},
                    {"DarkTheme", "Dark Theme"},
                    {"SelectWindow", "Select a window first!"},
                    {"InvalidValues", "Enter valid numeric values!"},
                    {"PositiveValues", "Width and height must be greater than zero!"},
                    {"Resized", "Window '{0}' resized to {1}x{2}!"},
                    {"Donation", "Donation"}
                }
            },
            {
                "中文 (Simplified Chinese)", new Dictionary<string, string>
                {
                    {"Title", "NanoPlay"},
                    {"Width", "宽度:"},
                    {"Height", "高度:"},
                    {"Resize", "调整大小"},
                    {"Refresh", "刷新"},
                    {"DarkTheme", "深色主题"},
                    {"SelectWindow", "请先选择一个窗口！"},
                    {"InvalidValues", "请输入有效的数值！"},
                    {"PositiveValues", "宽度和高度必须大于零！"},
                    {"Resized", "窗口 '{0}' 已调整为 {1}x{2}！"},
                    {"Donation", "捐款"}
                }
            },
            {
                "Español", new Dictionary<string, string>
                {
                    {"Title", "NanoPlay"},
                    {"Width", "Ancho:"},
                    {"Height", "Alto:"},
                    {"Resize", "Redimensionar"},
                    {"Refresh", "Actualizar"},
                    {"DarkTheme", "Tema Oscuro"},
                    {"SelectWindow", "¡Selecciona una ventana primero!"},
                    {"InvalidValues", "¡Ingresa valores numéricos válidos!"},
                    {"PositiveValues", "¡El ancho y la altura deben ser mayores que cero!"},
                    {"Resized", "¡Ventana '{0}' redimensionada a {1}x{2}!"},
                    {"Donation", "Donación"}
                }
            },
            {
                "Português", new Dictionary<string, string>
                {
                    {"Title", "NanoPlay"},
                    {"Width", "Largura:"},
                    {"Height", "Altura:"},
                    {"Resize", "Redimensionar"},
                    {"Refresh", "Atualizar"},
                    {"DarkTheme", "Tema Escuro"},
                    {"SelectWindow", "Selecione uma janela primeiro!"},
                    {"InvalidValues", "Digite valores numéricos válidos!"},
                    {"PositiveValues", "Largura e altura devem ser maiores que zero!"},
                    {"Resized", "Janela '{0}' redimensionada para {1}x{2}!"},
                    {"Donation", "Doação"}
                }
            }
        };

        private string currentLanguage;

        public Form1()
        {
            InitializeComponent();
            SetDefaultLanguage();
            SetupUI();
            RefreshWindowList();
            ApplyTheme(true); // Tema escuro por padrão
            UpdateLanguage();
        }

        private void SetDefaultLanguage()
        {
            string systemLanguage = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower();
            switch (systemLanguage)
            {
                case "en":
                    currentLanguage = "English";
                    break;
                case "zh":
                    currentLanguage = "中文 (Simplified Chinese)";
                    break;
                case "es":
                    currentLanguage = "Español";
                    break;
                case "pt":
                    currentLanguage = "Português";
                    break;
                default:
                    currentLanguage = "English";
                    break;
            }
        }

        private void SetupUI()
        {
            this.Text = "NanoPlay";
            this.Width = 450;
            this.Height = 340;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Icon = new Icon("xp.ico"); // Define o ícone do formulário (certifique-se de que NanoPlay.ico está no projeto)

            // ComboBox para listar janelas
            windowList = new ComboBox
            {
                Location = new Point(20, 20),
                Width = 400,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10)
            };
            windowList.SelectedIndexChanged += WindowList_SelectedIndexChanged;
            this.Controls.Add(windowList);

            // Label e TextBox para largura
            widthLabel = new Label
            {
                Location = new Point(20, 70),
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            widthBox = new TextBox
            {
                Location = new Point(100, 68),
                Width = 60,
                Text = "320",
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10),
                TextAlign = HorizontalAlignment.Center
            };
            this.Controls.Add(widthLabel);
            this.Controls.Add(widthBox);

            // Label e TextBox para altura
            heightLabel = new Label
            {
                Location = new Point(180, 70),
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            heightBox = new TextBox
            {
                Location = new Point(260, 68),
                Width = 60,
                Text = "240",
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10),
                TextAlign = HorizontalAlignment.Center
            };
            this.Controls.Add(heightLabel);
            this.Controls.Add(heightBox);

            // Botão para redimensionar
            resizeButton = new Button
            {
                Location = new Point(20, 110),
                Width = 120,
                Height = 40,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            resizeButton.FlatAppearance.BorderSize = 0;
            resizeButton.Click += ResizeButton_Click;
            this.Controls.Add(resizeButton);

            // Botão para atualizar lista
            refreshButton = new Button
            {
                Location = new Point(150, 110),
                Width = 120,
                Height = 40,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            refreshButton.FlatAppearance.BorderSize = 0;
            refreshButton.Click += RefreshButton_Click;
            this.Controls.Add(refreshButton);

            // CheckBox para tema
            themeToggle = new CheckBox
            {
                Location = new Point(20, 160),
                AutoSize = true,
                Checked = true,
                Font = new Font("Segoe UI", 9)
            };
            themeToggle.CheckedChanged += ThemeToggle_CheckedChanged;
            this.Controls.Add(themeToggle);

            // ComboBox para idioma
            languageSelector = new ComboBox
            {
                Location = new Point(20, 190),
                Width = 150,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9)
            };
            languageSelector.Items.AddRange(new[] { "English", "中文 (Simplified Chinese)", "Español", "Português" });
            languageSelector.SelectedIndex = Array.IndexOf(languageSelector.Items.Cast<string>().ToArray(), currentLanguage);
            languageSelector.SelectedIndexChanged += LanguageSelector_SelectedIndexChanged;
            this.Controls.Add(languageSelector);

            // Botão de doação
            donationButton = new Button
            {
                Location = new Point(180, 190),
                Width = 120,
                Height = 40,
                Text = "Doação",
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.Purple,
                ForeColor = Color.White
            };
            donationButton.FlatAppearance.BorderSize = 0;
            donationButton.FlatAppearance.MouseOverBackColor = Color.MediumPurple; // Cor ao passar o mouse
            donationButton.Click += donationButton_Click;
            donationButton.MouseEnter += (s, e) => donationButton.BackColor = Color.MediumPurple;
            donationButton.MouseLeave += (s, e) => donationButton.BackColor = donationButton.Tag != null ? (Color)donationButton.Tag : Color.Purple;
            this.Controls.Add(donationButton);
        }

        private void ApplyTheme(bool isDark)
        {
            if (isDark)
            {
                this.BackColor = Color.FromArgb(30, 30, 30);
                this.ForeColor = Color.WhiteSmoke;
                windowList.BackColor = Color.FromArgb(50, 50, 50);
                windowList.ForeColor = Color.WhiteSmoke;
                widthBox.BackColor = Color.FromArgb(50, 50, 50);
                widthBox.ForeColor = Color.WhiteSmoke;
                heightBox.BackColor = Color.FromArgb(50, 50, 50);
                heightBox.ForeColor = Color.WhiteSmoke;
                widthLabel.ForeColor = Color.Cyan;
                heightLabel.ForeColor = Color.Cyan;
                resizeButton.BackColor = Color.FromArgb(0, 120, 215);
                resizeButton.ForeColor = Color.White;
                refreshButton.BackColor = Color.FromArgb(0, 120, 215);
                refreshButton.ForeColor = Color.White;
                themeToggle.ForeColor = Color.WhiteSmoke;
                languageSelector.BackColor = Color.FromArgb(50, 50, 50);
                languageSelector.ForeColor = Color.WhiteSmoke;
                donationButton.BackColor = Color.Purple;
                donationButton.ForeColor = Color.White;
                donationButton.Tag = Color.Purple; // Armazena a cor padrão para restaurar no MouseLeave
            }
            else
            {
                this.BackColor = Color.FromArgb(240, 240, 240);
                this.ForeColor = Color.Black;
                windowList.BackColor = Color.White;
                windowList.ForeColor = Color.Black;
                widthBox.BackColor = Color.White;
                widthBox.ForeColor = Color.Black;
                heightBox.BackColor = Color.White;
                heightBox.ForeColor = Color.Black;
                widthLabel.ForeColor = Color.FromArgb(0, 85, 170);
                heightLabel.ForeColor = Color.FromArgb(0, 85, 170);
                resizeButton.BackColor = Color.FromArgb(0, 120, 215);
                resizeButton.ForeColor = Color.White;
                refreshButton.BackColor = Color.FromArgb(0, 120, 215);
                refreshButton.ForeColor = Color.White;
                themeToggle.ForeColor = Color.Black;
                languageSelector.BackColor = Color.White;
                languageSelector.ForeColor = Color.Black;
                donationButton.BackColor = Color.FromArgb(128, 0, 128);
                donationButton.ForeColor = Color.White;
                donationButton.Tag = Color.FromArgb(128, 0, 128); // Armazena a cor padrão para restaurar no MouseLeave
            }
        }

        private void UpdateLanguage()
        {
            var lang = translations[currentLanguage];
            this.Text = lang["Title"];
            widthLabel.Text = lang["Width"];
            heightLabel.Text = lang["Height"];
            resizeButton.Text = lang["Resize"];
            refreshButton.Text = lang["Refresh"];
            themeToggle.Text = lang["DarkTheme"];
            donationButton.Text = lang["Donation"];
        }

        private void RefreshWindowList()
        {
            windows.Clear();
            EnumWindows((hWnd, lParam) =>
            {
                if (!IsWindowVisible(hWnd))
                    return true;

                System.Text.StringBuilder title = new System.Text.StringBuilder(256);
                int titleLength = GetWindowText(hWnd, title, title.Capacity);
                if (titleLength <= 0 || string.IsNullOrWhiteSpace(title.ToString()))
                    return true;

                System.Text.StringBuilder className = new System.Text.StringBuilder(256);
                GetClassName(hWnd, className, className.Capacity);

                string titleStr = title.ToString();
                string classStr = className.ToString();
                if (titleStr == "Program Manager" || classStr == "Shell_TrayWnd" || classStr == "Button" ||
                    classStr == "tooltips_class32" || classStr == "MSCTFIME UI" ||
                    titleStr == this.Text)
                    return true;

                windows.Add((hWnd, titleStr));
                return true;
            }, IntPtr.Zero);

            windowList.Items.Clear();
            foreach (var window in windows)
            {
                windowList.Items.Add(window.Title);
            }

            if (windowList.Items.Count > 0)
            {
                windowList.SelectedIndex = 0;
            }
        }

        private void ResizeButton_Click(object sender, EventArgs e)
        {
            var lang = translations[currentLanguage];
            if (windowList.SelectedIndex >= 0)
            {
                var selectedWindow = windows[windowList.SelectedIndex];
                if (int.TryParse(widthBox.Text, out int width) && int.TryParse(heightBox.Text, out int height))
                {
                    if (width > 0 && height > 0)
                    {
                        SetWindowPos(selectedWindow.Handle, HWND_TOP, 0, 0, width, height, SWP_SHOWWINDOW);
                        MessageBox.Show(string.Format(lang["Resized"], selectedWindow.Title, width, height),
                            "NanoPlay", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show(lang["PositiveValues"], "Erro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    MessageBox.Show(lang["InvalidValues"], "Erro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                MessageBox.Show(lang["SelectWindow"], "Erro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            RefreshWindowList();
        }

        private void WindowList_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Futuro: exibir info da janela selecionada
        }

        private void ThemeToggle_CheckedChanged(object sender, EventArgs e)
        {
            ApplyTheme(themeToggle.Checked);
        }

        private void LanguageSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            currentLanguage = languageSelector.SelectedItem.ToString();
            UpdateLanguage();
        }

        private void donationButton_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://nubank.com.br/cobrar/12h52y/677c3495-7226-4c16-96d5-a8d9ad033952",
                UseShellExecute = true
            });
        }
    }
}