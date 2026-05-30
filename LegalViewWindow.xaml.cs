using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Animation;

namespace BeiShuiCS2
{
    public enum LegalType
    {
        Agreement,
        Privacy,
        Declaration
    }

    public partial class LegalViewWindow : Window
    {
        public LegalViewWindow(LegalType type)
        {
            InitializeComponent();

            Loaded += (s, e) =>
            {
                this.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3)));
                AnimationHelper.CreateFloatingParticles(particleCanvas, 6);

                switch (type)
                {
                    case LegalType.Agreement:
                        txtTitle.Text = "用户协议";
                        txtContent.Text = LoadText("Resources.Legal.agreement.txt");
                        break;
                    case LegalType.Privacy:
                        txtTitle.Text = "隐私政策";
                        txtContent.Text = LoadText("Resources.Legal.privacy.txt");
                        break;
                    case LegalType.Declaration:
                        txtTitle.Text = "平台声明";
                        txtContent.Text = LoadText("Resources.Legal.declaration.txt");
                        break;
                }
            };
        }

        private static string LoadText(string resourcePath)
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var embeddedName = $"BeiShuiCS2.{resourcePath.Replace('\\', '.').Replace('/', '.')}";
                using var stream = assembly.GetManifestResourceStream(embeddedName);
                if (stream != null)
                {
                    using var reader = new StreamReader(stream);
                    return reader.ReadToEnd();
                }
            }
            catch { }

            return "（无法加载文档内容，请联系管理员）";
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            AnimationHelper.WindowExit(this, () => Close());
        }
    }
}
