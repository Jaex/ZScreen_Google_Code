﻿#region License Information (GPL v2)

/*
    ZUploader - A program that allows you to upload images, texts or files
    Copyright (C) 2008-2011 ZScreen Developers

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; either version 2
    of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/

#endregion License Information (GPL v2)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using HelpersLib;

namespace ScreenCapture
{
    public class Surface : Form
    {
        public Image SurfaceImage { get; protected set; }

        public SurfaceOptions Config { get; set; }

        public Rectangle CurrentArea { get; protected set; }

        public int FPS { get; private set; }

        protected bool IsAreaCreated { get; set; }

        protected List<DrawableObject> DrawableObjects { get; set; }

        protected Point ClientMousePosition
        {
            get
            {
                return FixCursorPosition(MousePosition);
            }
        }

        private TextureBrush backgroundBrush;
        private Rectangle screenBounds, drawArea, drawAreaOneSmall;
        private Stopwatch timer;
        private int frameCount;

        protected GraphicsPath regionPath;
        protected Pen borderPen;
        protected Brush shadowBrush, lightBrush, nodeBackgroundBrush;
        protected Font textFont;
        protected Point mousePosition, oldMousePosition;
        protected bool isMouseDown, oldIsMouseDown;

        private bool isBottomRightMoving = true;

        public Surface(Image backgroundImage = null)
        {
            screenBounds = CaptureHelpers.GetScreenBounds();

            InitializeComponent();

            drawArea = new Rectangle(0, 0, screenBounds.Width, screenBounds.Height);
            drawAreaOneSmall = new Rectangle(0, 0, screenBounds.Width - 1, screenBounds.Height - 1);

            if (backgroundImage != null)
            {
                LoadBackground(backgroundImage);
            }

            Config = new SurfaceOptions();

            DrawableObjects = new List<DrawableObject>();

            timer = new Stopwatch();

            borderPen = new Pen(Color.DarkBlue);
            shadowBrush = new SolidBrush(Color.FromArgb(75, Color.Black));
            lightBrush = new SolidBrush(Color.FromArgb(10, Color.Black));
            nodeBackgroundBrush = new SolidBrush(Color.White);
            textFont = new Font("Arial", 18, FontStyle.Bold);

            MouseDoubleClick += new MouseEventHandler(Surface_MouseDoubleClick);
            MouseDown += new MouseEventHandler(Surface_MouseDown);
            MouseUp += new MouseEventHandler(Surface_MouseUp);
            KeyDown += new KeyEventHandler(Surface_KeyDown);
            KeyUp += new KeyEventHandler(Surface_KeyUp);
            Shown += new EventHandler(Surface_Shown);
        }

        private void Surface_Shown(object sender, System.EventArgs e)
        {
            Activate();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Bounds = screenBounds;
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
            this.Text = "RegionCapture";
#if !DEBUG
            this.TopMost = true;
#endif
            this.ResumeLayout(false);
        }

        public void LoadBackground(Image backgroundImage)
        {
            SurfaceImage = backgroundImage;
            backgroundBrush = new TextureBrush(backgroundImage);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
#if DEBUG
            if (!timer.IsRunning) timer.Start();
#endif

            Update();
            AfterUpdate();

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighSpeed;
            g.FillRectangle(backgroundBrush, drawArea);

#if DEBUG
            g.DrawRectangle(Pens.Yellow, drawAreaOneSmall);
#endif

            Draw(g);

#if DEBUG
            CheckFPS();
#endif

            DrawInfo(g);

            Invalidate();
        }

        public virtual Image GetRegionImage()
        {
            Image img = SurfaceImage;

            Rectangle newArea = Rectangle.Intersect(CurrentArea, drawArea);

            if (regionPath != null)
            {
                using (GraphicsPath gp = (GraphicsPath)regionPath.Clone())
                using (Matrix matrix = new Matrix())
                {
                    gp.CloseFigure();
                    RectangleF bounds = gp.GetBounds();
                    matrix.Translate(-bounds.X, -bounds.Y);
                    gp.Transform(matrix);

                    img = CaptureHelpers.CropImage(img, newArea, gp);

                    if (Config.DrawBorder)
                    {
                        img = CaptureHelpers.DrawBorder(img, gp);
                    }
                }

                if (Config.DrawChecker)
                {
                    img = CaptureHelpers.DrawCheckers(img);
                }
            }
            else
            {
                img = CaptureHelpers.CropImage(img, newArea);

                if (Config.DrawBorder)
                {
                    img = CaptureHelpers.DrawBorder(img);
                }
            }

            return img;
        }

        public void MoveArea(int x, int y)
        {
            CurrentArea = new Rectangle(new Point(CurrentArea.X + x, CurrentArea.Y + y), CurrentArea.Size);
        }

        public void ShrinkArea(int x, int y)
        {
            if (isBottomRightMoving)
            {
                CurrentArea = new Rectangle(CurrentArea.Left, CurrentArea.Top, CurrentArea.Width + x, CurrentArea.Height + y);
            }
            else
            {
                CurrentArea = new Rectangle(CurrentArea.Left + x, CurrentArea.Top + y, CurrentArea.Width - x, CurrentArea.Height - y);
            }
        }

        private void Surface_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Close();
            }
        }

        private void Surface_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isMouseDown = true;
            }
            else if (e.Button == MouseButtons.Right)
            {
                OnRightClickCancel();
            }
        }

        protected virtual void OnRightClickCancel()
        {
            if (IsAreaCreated)
            {
                IsAreaCreated = false;
                CurrentArea = Rectangle.Empty;
                HideNodes();
            }
            else
            {
                Close(true);
            }
        }

        private void Surface_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isMouseDown = false;

                if (Config.QuickCrop && !(this is PolygonRegion))
                {
                    Close();
                }
            }
        }

        private void Surface_KeyDown(object sender, KeyEventArgs e)
        {
            int speed;

            if (e.Control)
            {
                speed = Config.MaxMoveSpeed;
            }
            else
            {
                speed = Config.MinMoveSpeed;
            }

            switch (e.KeyCode)
            {
                case Keys.Left:
                    if (e.Shift) { MoveArea(-speed, 0); } else { ShrinkArea(-speed, 0); }
                    break;
                case Keys.Right:
                    if (e.Shift) { MoveArea(speed, 0); } else { ShrinkArea(speed, 0); }
                    break;
                case Keys.Up:
                    if (e.Shift) { MoveArea(0, -speed); } else { ShrinkArea(0, -speed); }
                    break;
                case Keys.Down:
                    if (e.Shift) { MoveArea(0, speed); } else { ShrinkArea(0, speed); }
                    break;
                case Keys.Tab:
                    isBottomRightMoving = !isBottomRightMoving;
                    break;
            }
        }

        private void Surface_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                Close(true);
            }
            else if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
            {
                Close();
            }
        }

        protected void Close(bool isCancel = false)
        {
            if (isCancel)
            {
                DialogResult = DialogResult.Cancel;
            }
            else
            {
                DialogResult = DialogResult.OK;
            }
        }

        private Point FixCursorPosition(Point position)
        {
            return new Point(position.X - screenBounds.X, position.Y - screenBounds.Y);
        }

        protected virtual new void Update()
        {
            mousePosition = ClientMousePosition;

            DrawableObject[] objects = DrawableObjects.OrderByDescending(x => x.Order).ToArray();

            if (objects.All(x => !x.IsDragging))
            {
                for (int i = 0; i < objects.Length; i++)
                {
                    DrawableObject obj = objects[i];

                    if (obj.IsMouseHover = obj.Rectangle.Contains(mousePosition))
                    {
                        for (int y = i + 1; y < objects.Length; y++)
                        {
                            objects[y].IsMouseHover = false;
                        }

                        break;
                    }
                }

                foreach (DrawableObject obj in objects)
                {
                    if (obj.IsMouseHover && !oldIsMouseDown && isMouseDown)
                    {
                        obj.IsDragging = true;
                        break;
                    }
                }
            }
            else
            {
                if (oldIsMouseDown && !isMouseDown)
                {
                    foreach (DrawableObject obj in objects)
                    {
                        obj.IsDragging = false;
                    }
                }
            }
        }

        protected virtual void AfterUpdate()
        {
            oldMousePosition = mousePosition;
            oldIsMouseDown = isMouseDown;
        }

        protected virtual void Draw(Graphics g)
        {
            DrawObjects(g);
        }

        protected void DrawObjects(Graphics g)
        {
            foreach (DrawableObject drawObject in DrawableObjects)
            {
                if (drawObject.Visible)
                {
                    drawObject.Draw(g);
                }
            }
        }

        private void CheckFPS()
        {
            frameCount++;

            if (timer.ElapsedMilliseconds >= 1000)
            {
                FPS = frameCount;
                frameCount = 0;
                timer.Reset();
                timer.Start();
                EverySecond();
            }
        }

        protected virtual void EverySecond()
        {
        }

        private void DrawInfo(Graphics g)
        {
            string text = string.Format("X: {0}, Y: {1}\nWidth: {2}, Height: {3}", CurrentArea.X, CurrentArea.Y, CurrentArea.Width, CurrentArea.Height);

#if DEBUG
            text = string.Format("FPS: {0}\nBounds: {1}\n{2}", FPS, screenBounds, text);
#endif

            SizeF textSize = g.MeasureString(text, textFont);

            int offset = 30;

            Rectangle primaryScreen = Screen.PrimaryScreen.Bounds;

            Point position = FixCursorPosition(new Point(primaryScreen.X + (int)(primaryScreen.Width / 2 - textSize.Width / 2), primaryScreen.Y + offset - 1));
            Rectangle rect = new Rectangle(position, new Size((int)textSize.Width, (int)textSize.Height));

            if (rect.Contains(mousePosition))
            {
                position = FixCursorPosition(new Point(primaryScreen.X + (int)(primaryScreen.Width / 2 - textSize.Width / 2),
                    primaryScreen.Y + primaryScreen.Height - (int)textSize.Height - offset - 1));
            }

            CaptureHelpers.DrawTextWithShadow(g, text, position, textFont, Color.White, Color.Black, 1);
        }

        protected Rectangle CalculateAreaFromNodes()
        {
            IEnumerable<NodeObject> nodes = DrawableObjects.OfType<NodeObject>().Where(x => x.Visible);

            if (nodes.Count() > 1)
            {
                int left, top, right, bottom;
                left = (int)nodes.Min(x => x.Position.X);
                top = (int)nodes.Min(x => x.Position.Y);
                right = (int)nodes.Max(x => x.Position.X);
                bottom = (int)nodes.Max(x => x.Position.Y);

                return CaptureHelpers.CreateRectangle(new Point(left, top), new Point(right, bottom));
            }

            return Rectangle.Empty;
        }

        protected void ShowNodes()
        {
            foreach (NodeObject node in DrawableObjects.OfType<NodeObject>())
            {
                node.Visible = true;
            }
        }

        protected void HideNodes()
        {
            foreach (NodeObject node in DrawableObjects.OfType<NodeObject>())
            {
                node.Visible = false;
            }
        }

        private IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            if (backgroundBrush != null) backgroundBrush.Dispose();
            if (regionPath != null) regionPath.Dispose();
            if (borderPen != null) borderPen.Dispose();
            if (shadowBrush != null) shadowBrush.Dispose();
            if (nodeBackgroundBrush != null) nodeBackgroundBrush.Dispose();
            if (textFont != null) textFont.Dispose();

            base.Dispose(disposing);
        }
    }
}