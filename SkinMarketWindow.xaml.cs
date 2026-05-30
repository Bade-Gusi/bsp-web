using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace BeiShuiCS2
{
    public partial class SkinMarketWindow : Window
    {
        private List<SkinItem> skins = new();

        public SkinMarketWindow()
        {
            InitializeComponent();
            this.Loaded += (s, e) => this.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.4)));
            this.MouseLeftButtonDown += (s, e) =>
            {
                if (e.GetPosition(this).Y < 64) this.DragMove();
            };

            LoadSkins();
        }

        private void LoadSkins()
        {
            skins.Add(new SkinItem { Name = "龙狙", Weapon = "AWP", Rarity = "神话", Price = 5000, Color = "#FFD700" });
            skins.Add(new SkinItem { Name = "火神", Weapon = "AK-47", Rarity = "传说", Price = 2500, Color = "#FF6B35" });
            skins.Add(new SkinItem { Name = "暴怒野兽", Weapon = "M4A4", Rarity = "史诗", Price = 800, Color = "#A855F7" });
            skins.Add(new SkinItem { Name = "二西莫夫", Weapon = "M4A1-S", Rarity = "史诗", Price = 600, Color = "#3B82F6" });
            skins.Add(new SkinItem { Name = "红线", Weapon = "AK-47", Rarity = "罕见", Price = 150, Color = "#EF4444" });
            skins.Add(new SkinItem { Name = "精英之作", Weapon = "M4A4", Rarity = "普通", Price = 50, Color = "#94A3B8" });
            skins.Add(new SkinItem { Name = "多普勒", Weapon = "蝴蝶刀", Rarity = "神话", Price = 8000, Color = "#22D3EE" });
            skins.Add(new SkinItem { Name = "虎牙", Weapon = "爪子刀", Rarity = "传说", Price = 3500, Color = "#F59E0B" });

            RenderSkins(skins);
        }

        private void RenderSkins(List<SkinItem> skinList)
        {
            skinContainer.Children.Clear();
            for (int i = 0; i < skinList.Count; i++)
            {
                var skin = skinList[i];
                var card = new Border
                {
                    Style = (Style)FindResource("SkinCard"),
                    Opacity = 0,
                    RenderTransform = new ScaleTransform(1, 1)
                };

                var stack = new StackPanel();

                // 武器图标区域
                var iconBorder = new Border
                {
                    Width = 168,
                    Height = 120,
                    CornerRadius = new CornerRadius(8),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0A0F0A")),
                    Margin = new Thickness(0, 0, 0, 12)
                };
                var iconText = new TextBlock
                {
                    Text = skin.Weapon,
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(skin.Color)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                iconBorder.Child = iconText;

                // 名称
                var nameText = new TextBlock
                {
                    Text = skin.Name,
                    FontSize = 16,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8F5E9")),
                    Margin = new Thickness(0, 0, 0, 4)
                };

                // 稀有度
                var rarityText = new TextBlock
                {
                    Text = skin.Rarity,
                    FontSize = 12,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(skin.Color)),
                    Margin = new Thickness(0, 0, 0, 8)
                };

                // 价格
                var priceGrid = new Grid();
                priceGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                priceGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var priceText = new TextBlock
                {
                    Text = $"{skin.Price:N0}",
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4ADE80"))
                };
                Grid.SetColumn(priceText, 0);

                var pointText = new TextBlock
                {
                    Text = " 点",
                    FontSize = 12,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(0, 0, 0, 2)
                };
                Grid.SetColumn(pointText, 1);

                priceGrid.Children.Add(priceText);
                priceGrid.Children.Add(pointText);

                stack.Children.Add(iconBorder);
                stack.Children.Add(nameText);
                stack.Children.Add(rarityText);
                stack.Children.Add(priceGrid);

                // 内层Border用于入场位移动画（TranslateTransform）
                // 外层Border的ScaleTransform由Style.Trigger控制hover缩放
                var innerBorder = new Border
                {
                    RenderTransform = new TranslateTransform(0, 30)
                };
                innerBorder.Child = stack;
                card.Child = innerBorder;
                skinContainer.Children.Add(card);

                // 交错进入动画
                var delay = TimeSpan.FromSeconds(0.05 * i);
                var opacityAnim = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.4))
                {
                    BeginTime = delay,
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                var translateAnim = new DoubleAnimation(30, 0, TimeSpan.FromSeconds(0.5))
                {
                    BeginTime = delay,
                    EasingFunction = new BackEase { Amplitude = 0.2, EasingMode = EasingMode.EaseOut }
                };

                card.BeginAnimation(OpacityProperty, opacityAnim);
                ((Border)card.Child).RenderTransform.BeginAnimation(TranslateTransform.YProperty, translateAnim);
            }
        }

        private void FilterAll_Click(object sender, RoutedEventArgs e) => RenderSkins(skins);
        private void FilterRifle_Click(object sender, RoutedEventArgs e) =>
            RenderSkins(skins.Where(s => s.Weapon.Contains("AK") || s.Weapon.Contains("M4") || s.Weapon.Contains("AWP")).ToList());
        private void FilterPistol_Click(object sender, RoutedEventArgs e) =>
            RenderSkins(skins.Where(s => s.Weapon.Contains("Glock") || s.Weapon.Contains("USP")).ToList());
        private void FilterKnife_Click(object sender, RoutedEventArgs e) =>
            RenderSkins(skins.Where(s => s.Weapon.Contains("刀")).ToList());

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.2));
            fadeOut.Completed += (s, _) => this.Close();
            this.BeginAnimation(OpacityProperty, fadeOut);
        }
    }

    public class SkinItem
    {
        public string Name { get; set; } = "";
        public string Weapon { get; set; } = "";
        public string Rarity { get; set; } = "";
        public int Price { get; set; }
        public string Color { get; set; } = "";
    }
}
