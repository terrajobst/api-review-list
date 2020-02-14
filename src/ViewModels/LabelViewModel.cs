using System;
using System.Windows.Media;

using Octokit;

namespace ApiReviewList.ViewModels
{
    internal sealed class LabelViewModel
    {
        public LabelViewModel(Label model)
        {
            int PerceivedBrightness(Color c)
            {
                return (int)Math.Sqrt(
                c.R * c.R * .241 +
                c.G * c.G * .691 +
                c.B * c.B * .068);
            }

            var backgroundColor = (Color)ColorConverter.ConvertFromString("#" + model.Color);
            var foregroundColor = (PerceivedBrightness(backgroundColor) > 130 ? Colors.Black : Colors.White);

            Foreground = new SolidColorBrush(foregroundColor);
            Background = new SolidColorBrush(backgroundColor);
            Text = model.Name;
        }

        public Brush Foreground { get; }
        public Brush Background { get; }
        public string Text { get; }
    }
}
