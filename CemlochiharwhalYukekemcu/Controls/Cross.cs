using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WpfApp1.Controls
{
	public class CrossReleasedEventArgs : EventArgs
	{
		internal Point Point { get; set; }
	}
	public class Cross : Control
	{
		private bool Dragginng = false;
		public event EventHandler<CrossReleasedEventArgs> CrossReleased;
		static Cross()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(Cross), new FrameworkPropertyMetadata(typeof(Cross)));
		}
		public Cross()
		{
		}

		protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			base.OnMouseLeftButtonDown(e);
			CaptureMouse();
			Dragginng = true;
			Opacity = 0;
		}
		protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
		{
			base.OnPreviewMouseLeftButtonUp(e);
			if (Dragginng)
			{
				ReleaseMouseCapture();
				Dragginng = false;
				CrossReleased?.Invoke(this, new CrossReleasedEventArgs() { Point = e.GetPosition(this) });
				Opacity = 1;
			}
		}
		protected override void OnMouseEnter(MouseEventArgs e)
		{
			base.OnMouseEnter(e);
			Cursor = Cursors.Cross;
		}
		protected override void OnMouseLeave(MouseEventArgs e)
		{
			base.OnMouseLeave(e);
			if (!Dragginng)
				Cursor = Cursors.Arrow;
		}

	}
}
