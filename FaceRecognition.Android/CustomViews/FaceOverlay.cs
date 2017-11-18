using System;
using System.Collections.Generic;

using Android.Content;
using Android.Views;
using Android.Gms.Vision;
using Android.Util;
using Android.Graphics;

namespace FaceRecognition.Droid
{
    public class FaceOverlay : View
    {
        private Object locker = new Object();
        private int previewWidth;
        private float mWidthScaleFactor = 1.0f;
        private int mPreviewHeight;
        private float mHeightScaleFactor = 1.0f;
        private CameraFacing mFacing = CameraFacing.Front;
        private HashSet<Graphic> mGraphics = new HashSet<Graphic>();

        public int PreviewWidth { get => previewWidth; set => previewWidth = value; }
        public float WidthScaleFactor { get => mWidthScaleFactor; set => mWidthScaleFactor = value; }
        public int PreviewHeight { get => mPreviewHeight; set => mPreviewHeight = value; }
        public float HeightScaleFactor { get => mHeightScaleFactor; set => mHeightScaleFactor = value; }
        public CameraFacing CameraFacing { get => mFacing; set => mFacing = value; }

        public FaceOverlay(Context context, IAttributeSet attrs) : base(context, attrs)
        {
        }

        /// <summary>
        /// Removes all graphics from the overlay.
        /// </summary>
        public void Clear()
        {
            lock(locker) {
                mGraphics.Clear();
            }
            PostInvalidate();
        }

        /// <summary>
        /// Adds a graphic to the overlay.
        /// </summary>
        /// <param name="graphic"></param>
        public void Add(Graphic graphic)
        {
            lock(locker) {
                mGraphics.Add(graphic);
            }
            PostInvalidate();
        }

        /// <summary>
        /// Removes a graphic from the overlay.
        /// </summary>
        /// <param name="graphic"></param>
        public void Remove(Graphic graphic)
        {
            lock(locker) {
                mGraphics.Remove(graphic);
            }
            PostInvalidate();
        }
       
        /// <summary>
        ///  Sets the camera attributes for size and facing direction, which informs how to transform image coordinates later.
        /// </summary>
        /// <param name="previewWidth"></param>
        /// <param name="previewHeight"></param>
        /// <param name="facing"></param>
        public void SetCameraInfo(int previewWidth, int previewHeight, CameraFacing facing)
        {
            lock(locker) {
                PreviewWidth = previewWidth;
                PreviewHeight = previewHeight;
                CameraFacing = facing;
            }
            PostInvalidate();
        }

        public override void Draw(Canvas canvas)
        {
            base.Draw(canvas);
            lock(locker) {
                if ((PreviewWidth != 0) && (PreviewHeight != 0))
                {
                    WidthScaleFactor = (float)canvas.Width / (float)PreviewWidth;
                    HeightScaleFactor = (float)canvas.Height / (float)PreviewHeight;
                }

                foreach (Graphic graphic in mGraphics)
                {
                    graphic.Draw(canvas);
                }
            }
        }
    }

    /**
     * Base class for a custom graphics object to be rendered within the graphic overlay.  Subclass
     * this and implement the {@link Graphic#draw(Canvas)} method to define the
     * graphics element.  Add instances to the overlay using {@link GraphicOverlay#add(Graphic)}.
     */
    public abstract class Graphic
    {
        private FaceOverlay mOverlay;

        public Graphic(FaceOverlay overlay)
        {
            mOverlay = overlay;
        }

        /**
         * Draw the graphic on the supplied canvas.  Drawing should use the following methods to
         * convert to view coordinates for the graphics that are drawn:
         * <ol>
         * <li>{@link Graphic#scaleX(float)} and {@link Graphic#scaleY(float)} adjust the size of
         * the supplied value from the preview scale to the view scale.</li>
         * <li>{@link Graphic#translateX(float)} and {@link Graphic#translateY(float)} adjust the
         * coordinate from the preview's coordinate system to the view coordinate system.</li>
         * </ol>
         *
         * @param canvas drawing canvas
         */
        public abstract void Draw(Canvas canvas);

        /**
         * Adjusts a horizontal value of the supplied value from the preview scale to the view
         * scale.
         */
        public float ScaleX(float horizontal)
        {
            return horizontal * mOverlay.WidthScaleFactor;
        }

        /**
         * Adjusts a vertical value of the supplied value from the preview scale to the view scale.
         */
        public float ScaleY(float vertical)
        {
            return vertical * mOverlay.HeightScaleFactor;
        }

        /**
         * Adjusts the x coordinate from the preview's coordinate system to the view coordinate
         * system.
         */
        public float TranslateX(float x)
        {
            if (mOverlay.CameraFacing == CameraFacing.Front)
            {
                return mOverlay.Width - ScaleX(x);
            }
            else
            {
                return ScaleX(x);
            }
        }

        /**
         * Adjusts the y coordinate from the preview's coordinate system to the view coordinate
         * system.
         */
        public float TranslateY(float y)
        {
            return ScaleY(y);
        }

        public void PostInvalidate()
        {
            mOverlay.PostInvalidate();
        }
    }
}