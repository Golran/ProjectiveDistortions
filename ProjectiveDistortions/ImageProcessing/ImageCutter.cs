using ProjectiveDistortions.Types;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectiveDistortions.ImageProcessing
{
    public static class ImageCutter
    {
        /// <summary>
        /// Отделяет изображение документа от фона.
        /// </summary>
        /// <param name="grayscale">Изображение в оттенках серого с выделенным и выравненным документом.</param>
        public static GrayscaleImage CutDocument(GrayscaleImage grayscale)
        {
            var distanceToDocument = GetDistanceToDocument(grayscale);
            var topDistance = distanceToDocument[0];
            var rightDistance = distanceToDocument[1];
            var downDistance = distanceToDocument[2];
            var leftDistance = distanceToDocument[3];


            var width = grayscale.Width;
            var height = grayscale.Height;
            var newWidth = rightDistance - leftDistance;
            var newHeight = downDistance - topDistance;
            if (newWidth % 4 != 0)
            {
                rightDistance -= newWidth % 4;
                newWidth = rightDistance - leftDistance;

            }
            var newColors = new byte[newWidth * newHeight];

            var pRes = Parallel.For(0, height, y =>
              {
                  for (int x = 0; x < width; x++)
                  {
                      if (y >= topDistance && y < downDistance && x >= leftDistance && x < rightDistance)
                      {
                          newColors[(y - topDistance) * newWidth + (x - leftDistance)] = grayscale.Colors[y * width + x];
                      }
                  }
              });
            if (!pRes.IsCompleted)
                throw new Exception("Parallel error");
            return ImageFilters.MedianFilterMod2(new GrayscaleImage(newWidth, newHeight, newColors));

        }

        /// <summary>
        /// Вычисление расстояний до границ документа.
        /// </summary>
        /// <param name="grayscale">Изображение в оттенках серого с выделенным и выравненным документом.</param>
        private static List<int> GetDistanceToDocument(GrayscaleImage grayscale)
        {
            var width = grayscale.Width;
            var height = grayscale.Height;
            var result = new List<int>();
            byte pixel;
            // После обработки, выделения фона и выравнивания изображения в фоне содержатся только пиксели белого
            // и чёрного цвета, тут можно придумать что-нибудь получше чем есть сейчас(просто ищем координаты первых
            // попавшихся пикселей по центру изображения со всех сторон по очереди).
            for (int y = 0; y < height; y++)
            {
                pixel = grayscale.Colors[y * width + width / 2];
                if (pixel == 0 || pixel == 255)
                    continue;
                result.Add(y);
                break;
            }
            for (int x = width - 1; x > -1; x--)
            {
                pixel = grayscale.Colors[(height / 2) * width + x];
                if (pixel == 0 || pixel == 255)
                    continue;
                result.Add(x);
                break;
            }
            for (int y = height - 1; y > -1; y--)
            {
                pixel = grayscale.Colors[y * width + width / 2];
                if (pixel == 0 || pixel == 255)
                    continue;
                result.Add(y);
                break;
            }
            for (int x = 0; x < width; x++)
            {
                pixel = grayscale.Colors[(height / 2) * width + x];
                if (pixel == 0 || pixel == 255)
                    continue;
                result.Add(x);
                break;
            }
            return result;
        }
    }
}
