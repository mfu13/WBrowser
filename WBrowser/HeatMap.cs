using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace WBrowser
{
    class HeatMap
    {
        /** half of the size of the circle picture. */
        private static  int          HALFCIRCLEPICSIZE = 32;
        /** path to picture of circle which gets more transparent to the outside. */
        private static String CIRCLEPIC = System.IO.Directory.GetCurrentDirectory()
                                                                    + "\\heatmap"
                                                                    + "\\bolilla.png";
        private static String SPECTRUMPIC = System.IO.Directory.GetCurrentDirectory()
                                                                    + "\\heatmap"
                                                                    + "\\colors.png";
        /** map to collect and sort points. */
        private Dictionary<int, List<Point>> map;
        /** maximum occurance of the same coordinates. */
        private int                       maxOccurance      = 1;
        /** maximal given x value. */
        private int                       maxXValue;
        /** maximal given y value. */
        private int                       maxYValue;
        /** name of file over which the heatmap will be laid. */
        private  String              lvlMap;
        /** name of file to save heatmap to. */
        private  String              outputFile;

        /**
         * constructs new instance of HeatMap from given list of points. Depending
         * on the amount of points, this may take a while, as the points are being
         * sorted.
         * 
         * @param points
         *            the list of points
         * @param output
         *            name of file to store created heatmap in
         * @param lvlMap
         *            name of file to lay heatmap over
         */
        public HeatMap(List<Point> points, String output,
                 String lvlMap) {
            outputFile = output;
            this.lvlMap = lvlMap;
            initMap(points);
        }


        /**
         * initiate map. counts and sorts points and figures out max x and y values
         * as well as the maximal amount of points with same coordinates. max x and
         * y values will be used for the size of the heatmap.
         * 
         * @param points
         *            list of points
         */
        private void initMap( List<Point> points) {
            map = new Dictionary<int, List<Point>>();
            Bitmap mapPic = loadImage(lvlMap);
            maxXValue = mapPic.Width;
            maxYValue = mapPic.Height;

            int pointSize = points.Count;
            for (int i = 0; i < pointSize; i++) {
                Point point = points.ElementAt(i);
                // add point to correct list.
                int hash = getkey(point);
                if (map.ContainsKey(hash)) {
                    List<Point> thisList = map[hash];
                    thisList.Add(point);
                    if (thisList.Count> maxOccurance) {
                        maxOccurance = thisList.Count;
                    }
                    // if list did not exist, create new one and add point.
                } else {
                    List<Point> newList = new List<Point>();
                    newList.Add(point);
                    map.Add(hash, newList);
                }
            }
        }

        /**
         * creates the heatmap.
         * 
         * @param multiplier
         *            calculated opacity of every point will be multiplied by this
         *            value. This leads to a HeatMap that is easier to read,
         *            especially when there are not too many points or the points
         *            are too spread out. Pass 1.0f for original.
         */
        public void createHeatMap( float multiplier) {

            Bitmap circle = loadImage(CIRCLEPIC);
            Bitmap heatMap = new Bitmap(maxXValue, maxYValue);
            paintInColor(heatMap, Color.Aqua);

            IEnumerator<List<Point>> iterator = map.Values.GetEnumerator();
            while (iterator.MoveNext()) {
                List<Point> currentPoints = iterator.Current;

                // calculate opaqueness
                // based on number of occurences of current point
                float opaque = currentPoints.Count / (float) maxOccurance;

                // adjust opacity so the heatmap is easier to read
                opaque = opaque * multiplier;
                if (opaque > 1) {
                    opaque = 1;
                }

                Point currentPoint = currentPoints.ElementAt(0);

                // draw a circle which gets transparent from middle to outside
                // (which opaqueness is set to "opaque")
                // at the position specified by the center of the currentPoint
                addImage(heatMap, circle, opaque,
                        (currentPoint.X - HALFCIRCLEPICSIZE),
                        (currentPoint.Y - HALFCIRCLEPICSIZE));
            }
           // print("done adding points.");

            // negate the image
            heatMap = negateImage(heatMap);

            // remap black/white with color spectrum from white over red, orange,
            // yellow, green to blue
            remap(heatMap);

            // blend image over lvlMap at opacity 30%
            Bitmap output = loadImage(lvlMap);
            addImage(output, heatMap, 0.3f);

            // save image
            saveImage(output, outputFile);
            //print("done creating heatmap.");
        }

        /**
         * remaps black and white picture with colors. It uses the colors from
         * SPECTRUMPIC. The whiter a pixel is, the more it will get a color from the
         * bottom of it. Black will stay black.
         * 
         * @param heatMapBW
         *            black and white heat map
         */
        private void remap(Bitmap heatMapBW) {
             Bitmap colorGradiant = loadImage(SPECTRUMPIC);
             int width = heatMapBW.Width;
             int height = heatMapBW.Height;
             int gradientHight = colorGradiant.Height - 1;
            for (int i = 0; i < width; i++) {
                for (int j = 0; j < height; j++) {

                    // get heatMapBW color values:
                    Color RGB = heatMapBW.GetPixel(i, j);
                    int rGB = RGB.ToArgb();

                    // calculate multiplier to be applied to height of gradiant.
                    float multiplier = rGB & 0xff; // blue
                    multiplier *= ((rGB >> 8)) & 0xff; // green
                    multiplier *= (rGB >> 16) & 0xff; // red
                    multiplier /= 16581375; // 255f * 255f * 255f


                    // apply multiplier
                     int y = (int) (multiplier * gradientHight);

                    // remap values
                    // calculate new value based on whitenes of heatMap
                    // (the whiter, the more a color from the top of colorGradiant
                    // will be chosen.
                     Color mapedRGB = colorGradiant.GetPixel(0, y);
                    // set new value
                     heatMapBW.SetPixel(i, j, mapedRGB);
                }
            }
        }

        /**
         * returns a negated version of this image.
         * 
         * @param img
         *            buffer to negate
         * @return negated buffer
         */
        private Bitmap negateImage( Bitmap img) {

            Bitmap clone = (Bitmap)img.Clone();

            using (Graphics g = Graphics.FromImage(img))
            {

                // negation ColorMatrix
                ColorMatrix colorMatrix = new ColorMatrix(
                    new float[][]
                        {
                            new float[] {-1, 0, 0, 0, 0},
                            new float[] {0, -1, 0, 0, 0},
                            new float[] {0, 0, -1, 0, 0},
                            new float[] {0, 0, 0, 1, 0},
                            new float[] {0, 0, 0, 0, 1}
                        });

                ImageAttributes attributes = new ImageAttributes();

                attributes.SetColorMatrix(colorMatrix);

                g.DrawImage(clone, new Rectangle(0, 0, clone.Width, clone.Height),
                            0, 0, clone.Width, clone.Height, GraphicsUnit.Pixel, attributes);
            }
            return clone;
        }

        /**
         * changes all pixel in the buffer to the provided color.
         * 
         * @param buff
         *            buffer
         * @param color
         *            color
         */
        private void paintInColor( Bitmap buff,  Color color) {
            Graphics g2 = Graphics.FromImage(buff);
            SolidBrush blueBrush = new SolidBrush(color);
            g2.FillRectangle(blueBrush, 0, 0, buff.Width, buff.Height);
            g2.Dispose();
        }

        /**
         * changes the opacity of the image.
         * 
         * @param buff1
         *            buffer to change opacity
         * @param opaque
         *            new opacity
         */
        private void makeTransparent( Bitmap buff1,  float opaque) {

            //create a graphics object from the image  
            using (Graphics gfx = Graphics.FromImage(buff1))
            {

                //create a color matrix object  
                ColorMatrix matrix = new ColorMatrix();

                //set the opacity  
                matrix.Matrix33 = opaque;

                //create image attributes  
                ImageAttributes attributes = new ImageAttributes();

                //set the color(opacity) of the image  
                attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                //now draw the image  
                gfx.DrawImage(buff1, new Rectangle(0, 0, buff1.Width, buff1.Height), 0, 0, buff1.Width, buff1.Height, GraphicsUnit.Pixel, attributes);
            }
        }

        private void SetImageOpacity(Bitmap buff1, float opacity)
        {
            using (Graphics gfx = Graphics.FromImage(buff1))
            {

                    //create a color matrix object  
                    ColorMatrix matrix = new ColorMatrix();

                    //set the opacity  
                    matrix.Matrix33 = opacity;

                    //create image attributes  
                    ImageAttributes attributes = new ImageAttributes();

                    //set the color(opacity) of the image  
                    attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                    //now draw the image  
                    gfx.DrawImage(buff1, new Rectangle(0, 0, buff1.Width, buff1.Height), 0, 0, buff1.Width, buff1.Height, GraphicsUnit.Pixel, attributes);
            }
        }

        /**
         * prints the contents of buff2 on buff1 with the given opaque value
         * starting at position 0, 0.
         * 
         * @param buff1
         *            buffer
         * @param buff2
         *            buffer to add to buff1
         * @param opaque
         *            opacity
         */
        private void addImage( Bitmap buff1,  Bitmap buff2, float opaque) {
            addImage(buff1, buff2, opaque, 0, 0);
        }

        /**
         * prints the contents of buff2 on buff1 with the given opaque value.
         * 
         * @param buff1
         *            buffer
         * @param buff2
         *            buffer
         * @param opaque
         *            how opaque the second buffer should be drawn
         * @param x
         *            x position where the second buffer should be drawn
         * @param y
         *            y position where the second buffer should be drawn
         */
        private void addImage(Bitmap buff1, Bitmap buff2, float opaque, int x, int y)
        {
            using (Graphics gfx = Graphics.FromImage(buff1))
            {

                //create a color matrix object  
                ColorMatrix matrix = new ColorMatrix();

                //set the opacity  
                matrix.Matrix33 = opaque;

                //create image attributes  
                ImageAttributes attributes = new ImageAttributes();

                //set the color(opacity) of the image  
                attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                //now draw the image  
                gfx.DrawImage(buff2, new Rectangle(0, 0, buff1.Width, buff1.Height), x, y, buff1.Width, buff1.Height, GraphicsUnit.Pixel, attributes);
            }
        }

        public Bitmap ChangeOpacity(Image img, float opacityvalue)
        {
            Bitmap bmp = new Bitmap(img.Width, img.Height); // Determining Width and Height of Source Image
            Graphics graphics = Graphics.FromImage(bmp);
            ColorMatrix colormatrix = new ColorMatrix();
            colormatrix.Matrix33 = opacityvalue;
            ImageAttributes imgAttribute = new ImageAttributes();
            imgAttribute.SetColorMatrix(colormatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
            graphics.DrawImage(img, new Rectangle(0, 0, bmp.Width, bmp.Height), 0, 0, img.Width, img.Height, GraphicsUnit.Pixel, imgAttribute);
            graphics.Dispose();   // Releasing all resource used by graphics 
            return bmp;
        }

        /**
         * saves the image in the provided buffer to the destination.
         * 
         * @param buff
         *            buffer to be saved
         * @param dest
         *            destination to save at
         */
        private void saveImage( Bitmap buff, String dest) {
            try {
                buff.Save(dest,ImageFormat.Png);
            } catch ( IOException e) {
                print("error saving the image: " + dest + ": " + e);
            }
        }

        private Bitmap loadImage( String refe) {
            Bitmap b1 = null;
            try {
                b1 = (Bitmap)Image.FromFile(refe);
            } catch ( IOException e) {
                System.Diagnostics.Debug.Write("error loading the image: " + refe + " : " + e);
            }
            return b1;
        }

        private int getkey( Point p) {
            return ((p.X << 19) | (p.Y << 7));
        }

      
        private void print( String s) {
            System.Diagnostics.Debug.Write(s);
        }
    }
}
