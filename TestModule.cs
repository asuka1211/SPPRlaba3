using NetTopologySuite.Geometries;
using OSMLSGlobalLibrary;
using OSMLSGlobalLibrary.Map;
using OSMLSGlobalLibrary.Modules;
using System;
using System.Collections.Generic;

namespace TestModule
{
    public class Task : OSMLSModule
    {
        public (int leftX, int rightX, int downY, int upY) map;

        Polygon polygon;

        List<(Ship, Coordinate)> ships = new List<(Ship, Coordinate)>();

        List<(int, int)> portCoordinates = new List<(int, int)>();
        // List<Coordinate[]> storms = new List<Coordinate[]>();
        List<Polygon> storms = new List<Polygon>();
        const int maximumShips = 1000;

        protected override void Initialize()
        {


            var rand = new Random();


            for (int i = 0; i < 10; i++)
            {
                AddStorm();
            }


            #region создание кастомного объекта и добавление на карту, модификация полигона заменой точки

            var countPort = 5;
            for (var i = 0; i < countPort; i++)
            {
                var lat = rand.Next(-70, 70);
                var lan = rand.Next(-70, 70);

                var portCoordinate = MathExtensions.LatLonToSpherMerc(lat, lan);
                MapObjects.Add(new Port(portCoordinate, 0));
                portCoordinates.Add((lat, lan));
            }

            for (int i = 0; i < 10; i++)
            {
                AddShip();
            }

            Console.WriteLine(ships.Count);

            #endregion
        }

        public void AddShip()
        {
            var sourcePort = portCoordinates[new Random().Next(portCoordinates.Count)];
            var destPort = portCoordinates[new Random().Next(portCoordinates.Count)];

            while (destPort == sourcePort)
            {
                destPort = portCoordinates[new Random().Next(portCoordinates.Count)];
            }

            var portCoordinate = MathExtensions.LatLonToSpherMerc(sourcePort.Item1, sourcePort.Item2);
            var destPortCoordinate = MathExtensions.LatLonToSpherMerc(destPort.Item1, destPort.Item2);

            var ship = new Ship(portCoordinate, 1000);

            ships.Add((ship, destPortCoordinate));
            MapObjects.Add(ship);
        }

        /// <summary>
        /// Вызывается постоянно, здесь можно реализовывать логику перемещений и всего остального, требующего времени.
        /// </summary>
        /// <param name="elapsedMilliseconds">TimeNow.ElapsedMilliseconds</param>
        public override void Update(long elapsedMilliseconds)
        {
            // Двигаем самолет.
            if (ships.Count < maximumShips)
            {
                AddShip();
            }

            var rand = new Random().Next(0, 10);

            if(rand == 5)
            {
                AddStorm();
            }

            if(rand == 3 && storms.Count > 0)
            {
                var pos = new Random().Next(0, storms.Count);
                MapObjects.Remove(storms[pos]);
                storms.RemoveAt(pos);
            }

            foreach (var ship in ships)
            {
                var realShip = ship.Item1;
                var realDest = ship.Item2;

                realShip.swimToPort(realDest, storms);
            }
        }
        public void AddStorm()
        {
            var rand = new Random();

            var lat = rand.Next(-70, 70);
            var lan = rand.Next(-70, 70);
            var portCoordinate = MathExtensions.LatLonToSpherMerc(lat, lan);


            var polygonCoordinates = new Coordinate[] {
                    portCoordinate,
                    MathExtensions.LatLonToSpherMerc(lat+5, lan+1),
                    MathExtensions.LatLonToSpherMerc(lat+3, lan-2),
                    MathExtensions.LatLonToSpherMerc(lat+2, lan-3),
                    portCoordinate,
                };
            // Создание стандартного полигона по ранее созданным координатам.
            polygon = new Polygon(new LinearRing(polygonCoordinates));
            MapObjects.Add(polygon);
            storms.Add(polygon);
        }
    }

    #region объявления класса, унаследованного от точки, объекты которого будут иметь уникальный стиль отображения на карте

    /// <summary>
    /// Самолет, умеющий летать вверх-вправо с заданной скоростью.
    /// </summary>
    [CustomStyle(
        @"new ol.style.Style({
            image: new ol.style.Circle({
                opacity: 1.0,
                scale: 1.0,
                radius: 3,
                fill: new ol.style.Fill({
                    color: 'rgba(255, 0, 255, 0.4)'
                }),
                stroke: new ol.style.Stroke({
                    color: 'rgba(0, 0, 0, 0.4)',
                    width: 1
                }),
            })
        });
        ")] // Переопределим стиль всех объектов данного класса, сделав самолет фиолетовым, используя атрибут CustomStyle.
    class Ship : Point // Унаследуем данный данный класс от стандартной точки.
    {
        /// <summary>
        /// Скорость корабля.
        /// </summary>
        public double Speed { get; }
        public Coordinate coordinate;

        /// <summary>
        /// Конструктор для создания нового объекта.
        /// </summary>
        /// <param name="coordinate">Начальные координаты.</param>
        /// <param name="speed">Скорость.</param>
        public Ship(Coordinate coordinate, double speed) : base(coordinate)
        {
            Speed = speed;
            this.coordinate = coordinate;
        }

        /// <summary>
        /// Двигает корабль вверх-вправо.
        /// </summary>
        internal void swimToPort(Coordinate coordinates, List<Polygon> storms)
        {

            double eps = 2 * Speed;
            foreach (var storm in storms)
            {
                foreach (var coordinatesStorm in storm.Coordinates)
                {
                    var absX = Math.Abs(coordinate.X - coordinatesStorm.X);
                    var absY = Math.Abs(coordinate.Y - coordinatesStorm.Y);

                    while (absX < eps)
                    {
                        absX = Math.Abs(coordinate.X - coordinatesStorm.X);
                        coordinate.X -= 5 * eps;
                    }

                    while (absY < eps)
                    {
                        absY = Math.Abs(coordinate.Y - coordinatesStorm.Y);
                        coordinate.Y -= 5 * eps;
                    }
                }
            }


            if (coordinate.X < coordinates.X)
            {
                X += Speed;
                
            }
            if (coordinate.X > coordinates.X)
            {
                X -= Speed;

         
            }
            if (coordinate.Y < coordinates.Y)
            {
                Y += Speed;
               
            }
            if (coordinate.Y > coordinates.Y)
            {
                Y -= Speed;
              
            }
         
        }

      
    }

    [CustomStyle(
        @"new ol.style.Style({
            image: new ol.style.Circle({
                opacity: 1.0,
                scale: 1.0,
                radius: 6,
                fill: new ol.style.Fill({
                    color: 'rgba(0, 0, 255, 0.4)'
                }),
                stroke: new ol.style.Stroke({
                    color: 'rgba(0, 0, 0, 0.4)',
                    width: 1
                }),
            })
        });
        ")]
    class Port : Point
    {
        public Coordinate coordinate;
        public double Speed { get; }


        public Port(Coordinate coordinate, double speed) : base(coordinate)
        {
            this.coordinate = coordinate;
            Speed = speed;

        }

    }

    #endregion
}