﻿#region using directives

using System;
using PoGo.NecroBot.Logic.Service.Elevation;
using PoGo.NecroBot.Logic.State;
using GeoCoordinatePortable;
using System.Threading.Tasks;

#endregion

namespace PoGo.NecroBot.Logic.Utils
{
    public static class LocationUtils
    {
        public static async Task UpdatePlayerLocationWithAltitude(ISession session,
            GeoCoordinate position, float speed)
        {
            double altitude = await session.ElevationService.GetElevation(position.Latitude, position.Longitude).ConfigureAwait(false);

            session.Client.Player.UpdatePlayerLocation(position.Latitude, position.Longitude, altitude, speed);
        }

        public static bool IsValidLocation(double latitude, double longitude)
        {
            return latitude <= 90 && latitude >= -90 && longitude >= -180 && longitude <= 180;
        }

        public static double CalculateDistanceInMeters(double sourceLat, double sourceLng,
                double destLat, double destLng)
            // from http://stackoverflow.com/questions/6366408/calculating-distance-between-two-latitude-and-longitude-geocoordinates
        {
            try
            {
                var sourceLocation = new GeoCoordinate(sourceLat, sourceLng);
                var targetLocation = new GeoCoordinate(destLat, destLng);
                return sourceLocation.GetDistanceTo(targetLocation);
            }
            catch (ArgumentOutOfRangeException)
            {
                return double.MaxValue;
            }
        }

        public static double CalculateDistanceInMeters(GeoCoordinate sourceLocation, GeoCoordinate destinationLocation)
        {
            return CalculateDistanceInMeters(sourceLocation.Latitude, sourceLocation.Longitude,
                destinationLocation.Latitude, destinationLocation.Longitude);
        }

        public static async Task<double> GetElevation(IElevationService elevationService, double lat, double lon)
        {
            if (elevationService != null)
                return await elevationService.GetElevation(lat, lon).ConfigureAwait(false);

            Random random = new Random();
            double maximum = 11.0f;
            double minimum = 8.6f;
            double return1 = random.NextDouble() * (maximum - minimum) + minimum;

            return return1;
        }

        public static async Task<GeoCoordinate> CreateWaypoint(GeoCoordinate sourceLocation,
                double distanceInMeters, double bearingDegrees)
            //from http://stackoverflow.com/a/17545955
        {
            var distanceKm = distanceInMeters / 1000.0;
            var distanceRadians = distanceKm / 6371; //6371 = Earth's radius in km

            var bearingRadians = ToRad(bearingDegrees);
            var sourceLatitudeRadians = ToRad(sourceLocation.Latitude);
            var sourceLongitudeRadians = ToRad(sourceLocation.Longitude);

            var targetLatitudeRadians = Math.Asin(Math.Sin(sourceLatitudeRadians) * Math.Cos(distanceRadians)
                                                  +
                                                  Math.Cos(sourceLatitudeRadians) * Math.Sin(distanceRadians) *
                                                  Math.Cos(bearingRadians));

            var targetLongitudeRadians = sourceLongitudeRadians + Math.Atan2(Math.Sin(bearingRadians)
                                                                             * Math.Sin(distanceRadians) *
                                                                             Math.Cos(sourceLatitudeRadians),
                                             Math.Cos(distanceRadians)
                                             - Math.Sin(sourceLatitudeRadians) * Math.Sin(targetLatitudeRadians));

            // adjust toLonRadians to be in the range -180 to +180...
            targetLongitudeRadians = (targetLongitudeRadians + 3 * Math.PI) % (2 * Math.PI) - Math.PI;

            return new GeoCoordinate(
                ToDegrees(targetLatitudeRadians),
                ToDegrees(targetLongitudeRadians),
                await GetElevation(null, sourceLocation.Latitude, sourceLocation.Longitude).ConfigureAwait(false)
            );
        }

        public static GeoCoordinate CreateWaypoint(GeoCoordinate sourceLocation, double distanceInMeters,
                double bearingDegrees, double altitude)
            //from http://stackoverflow.com/a/17545955
        {
            var distanceKm = distanceInMeters / 1000.0;
            var distanceRadians = distanceKm / 6371; //6371 = Earth's radius in km

            var bearingRadians = ToRad(bearingDegrees);
            var sourceLatitudeRadians = ToRad(sourceLocation.Latitude);
            var sourceLongitudeRadians = ToRad(sourceLocation.Longitude);

            var targetLatitudeRadians = Math.Asin(Math.Sin(sourceLatitudeRadians) * Math.Cos(distanceRadians)
                                                  +
                                                  Math.Cos(sourceLatitudeRadians) * Math.Sin(distanceRadians) *
                                                  Math.Cos(bearingRadians));

            var targetLongitudeRadians = sourceLongitudeRadians + Math.Atan2(Math.Sin(bearingRadians)
                                                                             * Math.Sin(distanceRadians) *
                                                                             Math.Cos(sourceLatitudeRadians),
                                             Math.Cos(distanceRadians)
                                             - Math.Sin(sourceLatitudeRadians) * Math.Sin(targetLatitudeRadians));

            // adjust toLonRadians to be in the range -180 to +180...
            targetLongitudeRadians = (targetLongitudeRadians + 3 * Math.PI) % (2 * Math.PI) - Math.PI;

            return new GeoCoordinate(ToDegrees(targetLatitudeRadians), ToDegrees(targetLongitudeRadians), altitude);
        }

        public static double DegreeBearing(GeoCoordinate sourceLocation, GeoCoordinate targetLocation)
            // from http://stackoverflow.com/questions/2042599/direction-between-2-latitude-longitude-points-in-c-sharp
        {
            var dLon = ToRad(targetLocation.Longitude - sourceLocation.Longitude);
            var dPhi = Math.Log(
                Math.Tan(ToRad(targetLocation.Latitude) / 2 + Math.PI / 4) /
                Math.Tan(ToRad(sourceLocation.Latitude) / 2 + Math.PI / 4));
            if (Math.Abs(dLon) > Math.PI)
                dLon = dLon > 0 ? -(2 * Math.PI - dLon) : 2 * Math.PI + dLon;
            return ToBearing(Math.Atan2(dLon, dPhi));
        }

        public static double ToBearing(double radians)
        {
            // convert radians to degrees (as bearing: 0...360)
            return (ToDegrees(radians) + 360) % 360;
        }

        public static double ToDegrees(double radians)
        {
            return radians * 180 / Math.PI;
        }

        public static double ToRad(double degrees)
        {
            return degrees * (Math.PI / 180);
        }
    }
}