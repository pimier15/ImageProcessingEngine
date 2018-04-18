﻿using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpeedyCoding;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using System.Drawing;
namespace EmguCvExtension
{
    public static class Processing
    {
        #region About Image Processing
        public static Image<Gray , byte> Inverse(
            this Image<Gray , byte> src )
        {
            var output = src.Clone();
            Parallel.For( 0 , output.Height , j =>
            {
                for ( int i = 0 ; i < output.Width ; i++ )
                {
                    output.Data [ j , i , 0 ] = ( byte )( 255 - src.Data [ j , i , 0 ] );
                }
            } );
            return output;
        }


        public static Image<TColor , TDepth> Inverse<TColor, TDepth>(
          this Image<TColor , TDepth> src )
          where TColor : struct, IColor
          where TDepth : new()
        {
            return src.Not();
        }

        public static Image<Gray , TDepth> Brightness<TDepth>(
            this Image<Gray , TDepth> src
            , double a
            , double b
            , double s
            , double E )
            where TDepth : new()
        {
            if ( a > 0 && b > 0 )
            {
                return ( src.Mul( 1 - s )
                            + s * a * ( src.Mul( 1 / a ).Pow( E ) ) )
                        .Pow( Math.Log( b ) / Math.Log( a ) );
            }

            return src;


            //return src.Mul(alpha).Add(new Gray(beta));
        }

        public static Image<Gray , TDepth> Brightness<TDepth>(
            this Image<Gray , TDepth> src
            , double alpha
            , double beta )
            where TDepth : new()
        {
            return src.Mul( alpha ).Add( new Gray( beta ) );
        }


        public static Image<Bgr , TDepth> Brightness<TDepth>(
           this Image<Bgr , TDepth> src
           , double alpha
           , int beta )
           where TDepth : new()
        {
            return src.Mul( alpha ).Add( new Bgr( beta , beta , beta ) );
        }

        public static Image<Gray , byte> Normalize(
           this Image<Gray , byte> src
           , byte max )
        {
            //var subimg = src.Resize(5000,5000,Emgu.CV.CvEnum.Inter.Nearest);
            //byte min = subimg.Data.Cast<byte>().Min();
            //byte max = subimg.Data.Cast<byte>().Max();
            byte min = 0;
            return src.Sub( new Gray( min ) ).Mul( 255.0 / ( double )( max - min ) );
        }

        public static Image<Gray , byte> AutoNormalize(
           this Image<Gray , byte> src )
        {
            var subimg = src.Resize(5000,5000,Emgu.CV.CvEnum.Inter.Nearest);
            byte min = subimg.Data.Cast<byte>().Min();
            byte max = subimg.Data.Cast<byte>().Max();
            return src.Sub( new Gray( min ) ).Mul( 255.0 / ( double )( max - min ) );
        }


        public static Image<Bgr , byte> Normalize(
           this Image<Bgr , byte> src
           , byte max )
        {
            byte min = 0;
            return src.Sub( new Bgr( min , min , min ) ).Mul( 255.0 / ( double )( max - min ) );
        }



        public static Image<Bgr , byte> Inverse(
          this Image<Bgr , byte> src )
        {
            return src.Not();
        }

        public static Image<Gray , byte> InvThres(
          this Image<Gray , byte> src
          , int thres )
        {

            for ( int j = 0 ; j < src.Data.GetLength( 0 ) ; j++ )
            {
                for ( int i = 0 ; i < src.Data.GetLength( 1 ) ; i++ )
                {
                    src.Data [ j , i , 0 ] = src.Data [ j , i , 0 ] > ( byte )thres ? ( byte )0 : src.Data [ j , i , 0 ];
                }
            }
            return src;
        }


        public static Image<TColor , TDepth> HistEqualize<TColor, TDepth>(
            this Image<TColor , TDepth> src )
            where TColor : struct, IColor
            where TDepth : new()
        {
            src._EqualizeHist();
            return src;
        }

        public static Image<TColor , TDepth> Gamma<TColor, TDepth>(
            this Image<TColor , TDepth> src
            , double gamma )
            where TColor : struct, IColor
            where TDepth : new()
        {
            src._GammaCorrect( gamma );
            return src;
        }


        public static Image<TColor , TDepth> Median<TColor, TDepth>(
           this Image<TColor , TDepth> src
           , int size )
           where TColor : struct, IColor
           where TDepth : new()
        {
            CvInvoke.MedianBlur( src , src , size );
            return src;
        }



        #endregion


        public static Func<Image<Gray , byte> , int , Image<Gray , byte>> FnMorp( morpOp op , kernal kernal )
        {
            var morp = new Func<Image<Gray,byte>, int, Image<Gray,byte>>(( img ,size) =>
            {
                if ( size == 0 ) size = 3;
                var ken = CreateKernal( kernal , new System.Drawing.Size(size,size ) );
                return img.MorphologyEx( CreateMorpOp(op) , ken,new System.Drawing.Point(-1,-1), 1 , BorderType.Default , new MCvScalar(0));
            });
            return morp;
        }


        public static Func<Image<Gray , byte> , int , Image<Gray , byte>> FnThreshold( ThresholdMode mode )
        {
            var thres = new Func<Image<Gray , byte> , int ,Image<Gray , byte>>((imgori , threshold) =>
            {
                var thresedimg = mode == ThresholdMode.Auto ? imgori.ThresholdAdaptive(new Gray(255),AdaptiveThresholdType.MeanC,ThresholdType.Binary,177,new Gray(2))
                                                            : imgori.ThresholdBinary(new Gray(threshold),new Gray(255));
                //var temptemp = imgori.ThresholdAdaptive(new Gray(255),AdaptiveThresholdType.MeanC,ThresholdType.Binary,157,new Gray(2));
                return thresedimg;
            });
            return thres;
        }
        public static Func<Image<Gray , byte> , Image<Gray , byte> , Image<Gray , byte>> FnTemplateMatch( TempMatchType method )
        {
            Func<TempMatchType,TemplateMatchingType> selectMethod = (( type )=>
            {
                switch ( type )
                {
                    case TempMatchType.Coeff:
                        return TemplateMatchingType.CcoeffNormed;
                    case TempMatchType.Corr:
                        return TemplateMatchingType.CcorrNormed;
                    case TempMatchType.Sq:
                        return TemplateMatchingType.SqdiffNormed;
                    default:
                        return TemplateMatchingType.CcoeffNormed;
                }
            });
            Func<Image<Gray , float> ,TempMatchType, Image<Gray , byte>> rescale = (( img , type)=>
            {
                switch ( type )
                {
                    case TempMatchType.Coeff:
                        return ( img * 255 ).Convert<Gray , byte>();
                    case TempMatchType.Corr:
                        return ( 255 - img * 255 ).Convert<Gray , byte>();
                    case TempMatchType.Sq:
                        return ( 255 - img * 255 ).Convert<Gray , byte>();
                    default:
                        return ( img * 255 ).Convert<Gray , byte>();
                }
            } );

            var templatematch = new Func<Image<Gray , byte> , Image<Gray , byte> , Image<Gray , byte>>((img,template)=>
            {
                var result = img.MatchTemplate( template , selectMethod(method) );
                return PaddingImage( rescale(result , method ) , template );
            } );
            return templatematch;
        }
        static Image<Gray , byte> PaddingImage( Image<Gray , byte> match_result , Image<Gray , byte> template )
        {
            Image<Gray,byte> padded = new Image<Gray, byte>(match_result.Width + template.Width , match_result.Height + template.Height);
            for ( int j = 0 ; j < match_result.Height ; j++ )
            {
                for ( int i = 0 ; i < match_result.Width ; i++ )
                {
                    padded [ template.Height / 2 + j , template.Width / 2 + i ] = match_result [ j , i ];
                }
            }

            //Parallel.For( 0 , match_result.Height , (j) =>
            //{
            //    for ( int i = 0 ; i < match_result.Width ; i++ )
            //    {
            //        padded[template.Height / 2 + j , template.Width / 2 + i] = match_result[j , i];
            //    }
            //} );

            return padded;
        }

        #region Check
        public static Func<System.Drawing.PointF , bool> FnInContour( VectorOfPoint contour )
        {
            var incontour = new Func<System.Drawing.PointF , bool>( ( pt ) =>
            {
                float ceilX = (float)Math.Ceiling( (double) pt.X);
                float ceilY = (float)Math.Ceiling( (double) pt.Y);
                float trunX = (float)Math.Truncate( (double) pt.X);
                float trunY = (float)Math.Truncate( (double) pt.Y);

                List<double> outlist = new List<double>();
                System.Drawing.PointF[] ptArr = new System.Drawing.PointF[4];

                ptArr[0] = new System.Drawing.PointF(ceilX,ceilY);
                ptArr[1] = new System.Drawing.PointF(trunX,ceilY);
                ptArr[2] = new System.Drawing.PointF(ceilX,trunY);
                ptArr[3] = new System.Drawing.PointF(trunX,trunY);

                for (int i = 0; i < ptArr.GetLength(0) ; i++)
                {
                    outlist.Add(CvInvoke.PointPolygonTest( contour , ptArr[i], false));
                }

                return outlist.Max() < 1? false : true ; // -1 outof contour , 0 : boundary contour, 1 : in contour
            } );
            return incontour;
        }
        public static Func<double , double , bool> FnInBox( Rectangle box , int margin )
        {
            var inbox = new Func<double , double , bool>((double y,double x)=> {
                if(x > box.X + box.Width + margin
                || x < box.X - margin
                || y > box.Y + box.Height + margin
                || y < box.Y - margin )
                    return false;
                return true;
            } );
            return inbox;
        }

        //  이제는 이걸로 박스 테크 한다. 
        public static List<Tuple<double,double,Nullable<Rectangle>>> InBoxList(
            this List<double [ ]>  srcYX
            , int margin
            , List<Rectangle> rectList )
        {
            return srcYX.Select( yx =>
            {
                var y = yx[0];
                var x = yx[1];

                bool inBox = false;
                Rectangle? box =  null;

                foreach ( var rect in rectList )
                {
                    if ( FnInBox( rect , margin )( y , x ) )
                    {
                        inBox = true;
                        box = rect;
                        break;
                    };
                }


                return Tuple.Create( yx [ 0 ]
                                     , yx [ 1 ]
                                     , inBox == true ? box : null );
            } ).ToList();
        }




        #endregion

        #region Calc

        public static Func<Rectangle , double> FnSumInsideBox( Image<Gray , byte> src )
        {
            var sumbox = new Func<Rectangle , double>((Rectangle box)=>
            {
                double sum = 0;
                for (int i = box.X; i < box.X + box.Width; i++)
                {
                    for (int j = box.Y; j < box.Y + box.Height; j++)
                    {
                        sum += src.Data[j,i,0];
                    }
                }
                return sum;
            } );
            return sumbox;
        }
        public static Func<int , int , double> FnSumAreaPoint( int height , int width , Image<Gray , byte> img )
        {
            var sumareap = new Func<int , int , double>((int y, int x)=>
            {
                double output = 0;
                for (int i = x - width/2; i < x+width/2; i++)
                {
                    for (int j = y - height/2; j < y+height/2; j++)
                    {
                        output += img.Data[ j, i, 0 ];
                    }
                }
                return output;
            } );
            return sumareap;
        }


        #endregion

        #region Etc

        /// <summary>
        /// Creat with Limit of area , Use only input image 
        /// </summary>
        /// <param name="areaUP"></param>
        /// <param name="areaDW"></param>
        /// <returns></returns>
        public static Func<Image<Gray , byte> , VectorOfVectorOfPoint> FnFindContour( double areaUP , double areaDW )
        {
            var findpasscntr = new Func<Image<Gray , byte> , VectorOfVectorOfPoint>((imgori) => {

                VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
                VectorOfVectorOfPoint passcontours = new VectorOfVectorOfPoint();
                CvInvoke.FindContours( imgori , contours , null , RetrType.External , ChainApproxMethod.ChainApproxNone );
                for ( int i = 0 ; i < contours.Size ; i++ )
                {
                    double areaSize = CvInvoke.ContourArea( contours[i],false);  //  Find the area of contour
                    if ( areaSize >= areaDW && areaSize <= areaUP )
                    {
                        passcontours.Push( contours[i] );
                    }
                }
                return passcontours;
            } );
            return findpasscntr;
        }

        public static Func<VectorOfVectorOfPoint , VectorOfVectorOfPoint> FnSortcontours()
        {
            var sort = new Func<VectorOfVectorOfPoint , VectorOfVectorOfPoint>((inputContours)=>
            {
                var temp = inputContours.ToArrayOfArray();
                Point[][] sorted = temp.OrderBy( p => p[0].Y ).ThenBy( p => p[0].X ).ToArray();
                return new VectorOfVectorOfPoint( sorted );
            } );
            return sort;
        }

        public static Func<VectorOfVectorOfPoint , List<Rectangle>> FnApplyBox( int upLimit , int dwLimit )
        {
            var applybox = new  Func<VectorOfVectorOfPoint , List<Rectangle>>((VectorOfVectorOfPoint contr)=>
            {
                List<System.Drawing.Rectangle> PassBoxArr = new List<System.Drawing.Rectangle>();
                for ( int i = 0 ; i < contr.Size ; i++ )
                {
                    System.Drawing.Rectangle rc = CvInvoke.BoundingRectangle(contr[i]);
                    PassBoxArr.Add( rc );
                }
                return PassBoxArr;
            } );
            return applybox;
        }

        public static Func<VectorOfVectorOfPoint , Point [ ]> FnCalcCenter()
        {
            return new Func<VectorOfVectorOfPoint , Point [ ]>( contours => {
                Point[] centerpoins = new Point[contours.Size];

                for ( int i = 0 ; i < contours.Size ; i++ )
                {
                    var moments = CvInvoke.Moments( contours[i] , false );
                    centerpoins [ i ] = new Point( ( int )( moments.M10 / moments.M00 )
                                                , ( int )( moments.M01 / moments.M00 ) );
                }

                return centerpoins;
            } );
        }

        public static Func<VectorOfVectorOfPoint,double[]> FnContours2Areas =
            contours => {
                double[] arealist = new double[contours.Size];
                for ( int i = 0 ; i < contours.Size ; i++ )
                {
                    arealist[i] = CvInvoke.ContourArea( contours[i] , false );
                }
                return arealist;
            };

        public static Func<Image<Gray,byte>,Image<Gray,byte>> FnOp_UnionTrans =
            img => {
                var data = img.Data;
                for (int j  = 0; j < img.Height; j++)
                {
                    for (int i = 0; i < img.Width; i++)
                    {
                        data[j,i,0] = data[j,i,0] > 127 ? (byte)((255 - data[j,i,0])*2) : (byte)(data[j,i,0]*2) ;
                    }
                }
                img.Data = data;
                return img;
            };
        #endregion

        #region Draw
        public static Action<double , double , dynamic> FnDrawCircle( int radius , Bgr color )
        {
            return new Action<double , double , dynamic>( ( y , x , image ) => {
                CircleF circle = new CircleF();
                circle.Center = new System.Drawing.PointF( ( float )x , ( float )y );
                circle.Radius = radius;
                image.Draw( circle , color , 1 );
            } );
        }

        public static Func<Image<Bgr , byte> , Image<Bgr , byte>> FnDrawWafer( int diameter , Bgr color , int thickness )
        {
            return new Func<Image<Bgr , byte> , Image<Bgr , byte>>( src =>
            {
                int flatzoneSize = diameter/10;
                var linecolor = new MCvScalar(color.Blue, color.Green, color.Red);
                var angle = Math.Asin((double)flatzoneSize / diameter) * 2.0;
                var flatzoneH = (double)flatzoneSize / ((Math.Tan(angle / 2) * 2f));

                Image<Bgr, byte> output = new Image<Bgr, byte>(src.Data);

                CvInvoke.Ellipse( output
                                  , new Point( src.Width / 2 , src.Height / 2 )
                                  , new Size( diameter / 2 , diameter / 2 )
                                  , 0f
                                  , 90 + angle.ToDegree() / 2
                                  , 360 + 90 - angle.ToDegree() / 2
                                  , linecolor
                                  , thickness );

                CvInvoke.Line( output
                               , new Point( src.Width / 2 - flatzoneSize / 2 , ( int )flatzoneH + src.Height / 2 )
                               , new Point( src.Width / 2 + flatzoneSize / 2 , ( int )flatzoneH + src.Height / 2 )
                               , linecolor
                               , thickness );

                return output;
            } );

        }


        public static Func<Image<Bgr , byte> , Image<Bgr , byte>> FnDrawWafer( int diameter , int flatzoneSize , Bgr color , int thickness )
        {
            return new Func<Image<Bgr , byte> , Image<Bgr , byte>>( src =>
            {
                var linecolor = new MCvScalar(color.Blue, color.Green, color.Red);
                var angle = Math.Asin((double)flatzoneSize / diameter) * 2.0;
                var flatzoneH = (double)flatzoneSize / ((Math.Tan(angle / 2) * 2f));

                Image<Bgr, byte> output = new Image<Bgr, byte>(src.Data);

                CvInvoke.Ellipse( output
                                  , new Point( src.Width / 2 , src.Height / 2 )
                                  , new Size( diameter / 2 , diameter / 2 )
                                  , 0f
                                  , 90 + angle.ToDegree() / 2
                                  , 360 + 90 - angle.ToDegree() / 2
                                  , linecolor
                                  , thickness );

                CvInvoke.Line( output
                               , new Point( src.Width / 2 - flatzoneSize / 2 , ( int )flatzoneH + src.Height / 2 )
                               , new Point( src.Width / 2 + flatzoneSize / 2 , ( int )flatzoneH + src.Height / 2 )
                               , linecolor
                               , thickness );

                return output;
            } );
        }

	



		public static Func<Image<Bgr , byte> , Image<Bgr , byte>> FnMatrixWafer( int diameter , int flatzoneSize , Bgr color , int thickness )
		{
			return new Func<Image<Bgr , byte> , Image<Bgr , byte>>( src =>
			{
				var linecolor = new MCvScalar(color.Blue, color.Green, color.Red);
				var angle = Math.Asin((double)flatzoneSize / diameter) * 2.0;
				var flatzoneH = (double)flatzoneSize / ((Math.Tan(angle / 2) * 2f));

				Image<Bgr, byte> output = new Image<Bgr, byte>(src.Data);

				CvInvoke.Ellipse( output
								  , new Point( src.Width / 2 , src.Height / 2 )
								  , new Size( diameter / 2 , diameter / 2 )
								  , 0f
								  , 90 + angle.ToDegree() / 2
								  , 360 + 90 - angle.ToDegree() / 2
								  , linecolor
								  , thickness );

				CvInvoke.Line( output
							   , new Point( src.Width / 2 - flatzoneSize / 2 , ( int )flatzoneH + src.Height / 2 )
							   , new Point( src.Width / 2 + flatzoneSize / 2 , ( int )flatzoneH + src.Height / 2 )
							   , linecolor
							   , thickness );

				return output;
			} );

		}


		#endregion

		#region Helper
		private static MorphOp CreateMorpOp( morpOp op )
        {
            switch ( op )
            {
                case morpOp.Erode:
                    return MorphOp.Erode;
                case morpOp.Dilate:
                    return MorphOp.Dilate;
                case morpOp.Open:
                    return MorphOp.Open;
                case morpOp.Close:
                    return MorphOp.Close;
                default:
                    return MorphOp.Erode;
            }
        }

        private static Mat CreateKernal( kernal kernal , System.Drawing.Size size )
        {
            switch ( kernal )
            {
                case kernal.Vertical:
                    var verisize = new System.Drawing.Size(1,size.Height);
                    return CvInvoke.GetStructuringElement( ElementShape.Rectangle , verisize , new System.Drawing.Point( -1 , -1 ) );
                case kernal.Horizontal:
                    var horisize = new System.Drawing.Size(size.Width,1);
                    return CvInvoke.GetStructuringElement( ElementShape.Rectangle , horisize , new System.Drawing.Point( -1 , -1 ) );
                case kernal.Cross:
                    return CvInvoke.GetStructuringElement( ElementShape.Cross , size , new System.Drawing.Point( -1 , -1 ) );
                case kernal.Rect:
                    return CvInvoke.GetStructuringElement( ElementShape.Rectangle , size , new System.Drawing.Point( -1 , -1 ) );
                default:
                    return CvInvoke.GetStructuringElement( ElementShape.Cross , size , new System.Drawing.Point( -1 , -1 ) );
            }
        }

        #endregion



    }
}
