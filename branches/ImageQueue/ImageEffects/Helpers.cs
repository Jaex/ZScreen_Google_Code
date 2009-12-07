﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing.Imaging;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace ImageEffects
{
    public static class Helpers
    {
        /*
        private const float rw = 0.3086f;
        private const float gw = 0.6094f;
        private const float bw = 0.0820f;
        */

        private const float rw = 0.212671f;
        private const float gw = 0.715160f;
        private const float bw = 0.072169f;

        public static void ApplyColorMatrix(Image img, ColorMatrix matrix)
        {
            using (Graphics g = Graphics.FromImage(img))
            {
                g.SmoothingMode = SmoothingMode.HighQuality;
                ImageAttributes imgattr = new ImageAttributes();
                imgattr.SetColorMatrix(matrix);
                g.DrawImage(img, new Rectangle(0, 0, img.Width, img.Height), 0, 0, img.Width, img.Height, GraphicsUnit.Pixel, imgattr);
            }
        }

        public static ColorMatrix BrightnessFilter(int percentage)
        {
            float perc = (float)percentage / 100;
            return new ColorMatrix(new[]{
                new float[] {1, 0, 0, 0, 0},
                new float[] {0, 1, 0, 0, 0},
                new float[] {0, 0, 1, 0, 0},
                new float[] {0, 0, 0, 1, 0},
                new float[] {perc, perc, perc, 0, 1}
            });
        }

        public static ColorMatrix Colorize(Color color, float percentage)
        {
            float r = (float)color.R / 255;
            float g = (float)color.G / 255;
            float b = (float)color.B / 255;
            float amount = percentage / 100;
            float inv_amount = 1 - amount;

            return new ColorMatrix(new[]{
                new float[] {inv_amount + amount * r * rw, amount * g * rw, amount * b * rw, 0, 0},
                new float[] {amount * r * gw, inv_amount + amount * g * gw, amount * b * gw, 0, 0},
                new float[] {amount * r * bw, amount * g * bw, inv_amount + amount * b * bw, 0, 0},
                new float[] {0, 0, 0, 1, 0},
                new float[] {0, 0, 0, 0, 1}});
        }

        public static ColorMatrix ContrastFilter(int percentage)
        {
            float perc = 1 + (float)percentage / 100;
            return new ColorMatrix(new[]{
                new float[] {perc, 0, 0, 0, 0},
                new float[] {0, perc, 0, 0, 0},
                new float[] {0, 0, perc, 0, 0},
                new float[] {0, 0, 0, 1, 0},
                new float[] {0, 0, 0, 0, 1}
            });
        }

        public static ColorMatrix GrayscaleFilter()
        {
            return new ColorMatrix(new[]{
                new float[] {rw, rw, rw, 0, 0},
                new float[] {gw, gw, gw, 0, 0},
                new float[] {bw, bw, bw, 0, 0},
                new float[] {0, 0, 0, 1, 0},
                new float[] {0, 0, 0, 0, 1}
            });
        }

        public static ColorMatrix Hue(float angle)
        {
            angle *= (float)(Math.PI / 180);
            float c = (float)Math.Cos(angle);
            float s = (float)Math.Sin(angle);

            return new ColorMatrix(new[]{
                new float[] {(rw + (c * (1 - rw))) + (s * -rw), (rw + (c * -rw)) + (s * 0.143f), (rw + (c * -rw)) + (s * -(1 - rw)), 0, 0},
                new float[] {(gw + (c * -gw)) + (s * -gw), (gw + (c * (1 - gw))) + (s * 0.14f), (gw + (c * -gw)) + (s * gw), 0, 0},
                new float[] {(bw + (c * -bw)) + (s * (1 - bw)), (bw + (c * -bw)) + (s * -0.283f), (bw + (c * (1 - bw))) + (s * bw), 0, 0},
                new float[] {0, 0, 0, 1, 0},
                new float[] {0, 0, 0, 0, 1}});
        }

        public static ColorMatrix InverseFilter()
        {
            return new ColorMatrix(new[]{
                new float[] {-1, 0, 0, 0, 0},
                new float[] {0, -1, 0, 0, 0},
                new float[] {0, 0, -1, 0, 0},
                new float[] {0, 0, 0, 1, 0},
                new float[] {1, 1, 1, 0, 1}
            });
        }

        public static ColorMatrix Saturation(float percentage)
        {
            float s = 1 + percentage / 100;

            return new ColorMatrix(new[]{
                new float[] {(1.0f - s) * rw + s, (1.0f - s) * rw, (1.0f - s) * rw, 0, 0},
                new float[] {(1.0f - s) * gw, (1.0f - s) * gw + s, (1.0f - s) * gw, 0, 0},
                new float[] {(1.0f - s) * bw, (1.0f - s) * bw, (1.0f - s) * bw + s, 0, 0},
                new float[] {0, 0, 0, 1, 0},
                new float[] {0, 0, 0, 0, 1}
            });
        }
    }
}