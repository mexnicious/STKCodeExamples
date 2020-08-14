﻿using System;
using System.Collections.Generic;
using System.Text;
using AGI.Ui.Plugins;
using AGI.STKObjects;
using System.Threading;
using BingMapsRESTToolkit;
//using Agi.Ui.GreatArc.BingGeocodeService;
//using Agi.Ui.GreatArc.BingRouteService;
using System.Configuration;
using System.IO;
using System.Windows.Forms; // for message boxes

namespace Agi.Ui.GreatArc
{
    class Directions
    {
        private AgStkObjectRoot m_root;
        private string installDir;

        public Directions(AgStkObjectRoot root)
        {
            m_root = root;
            AGI.STKUtil.IAgExecCmdResult result = m_root.ExecuteCommand("GetDirectory / STKHome");
            installDir = result[0];
            
        }

        // update these paths to match your setup
        // Bing maps keys are available here: http://msdn.microsoft.com/en-us/library/ff428642.aspx
        private string m_bingMapKey = "AiKngzZJC1qWk-jQ8moY9J0ZKsUPD70wmUxv-O6x2lPNGJrbaPNV_3JHnwE1KDrp";

        public struct MyWaypoint
        {
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public double Altitude { get; set; }
        }


        // UPDATED (old method left commented out below updated method)
        // convert Bing address to lat/lon
        public MyWaypoint Geocode(string address)
        {
            //MessageBox.Show($"Geocode method called for !! {address} "); // FOR DEBUGGING PURPOSES
            // create the Geocode request
            GeocodeRequest locationRequest = new GeocodeRequest
            {
                BingMapsKey = m_bingMapKey,
                MaxResults = 1,
                IncludeNeighborhood = true,
                IncludeIso2 = true,
                Query = address
            };

            //MessageBox.Show("About to execute location request!");// FOR DEBUGGING PURPOSES
            // execute the response
            // this call can also use await, but then method would have to be made async
            Response geocodeResponse = locationRequest.Execute().Result;
            //MessageBox.Show("Executed location Request!");// FOR DEBUGGING PURPOSES

            // set lat/lon/altitude of waypoint and return waypoint
            MyWaypoint returnPoint = new MyWaypoint();
            if (geocodeResponse.ResourceSets.Length > 0)
            {
                var result = geocodeResponse.ResourceSets[0].Resources[0] as BingMapsRESTToolkit.Location;
                returnPoint.Latitude = result.Point.Coordinates[0];
                returnPoint.Longitude = result.Point.Coordinates[1];
                returnPoint.Altitude = 0.0;
            }
            //MessageBox.Show("Geocode method done!");// FOR DEBUGGING PURPOSES
            return returnPoint;
        }
        // old method 
        /*     public MyWaypoint Geocode(string address)
             {
                 GeocodeRequest locationRequest = new GeocodeRequest();

                 // Set the credentials using a valid Bing Maps key
                 locationRequest.Credentials = new BingGeocodeService.Credentials();
                 locationRequest.Credentials.ApplicationId = m_bingMapKey;
                 locationRequest.Query = address;

                 // Make the calculate route request
                 GeocodeServiceClient geocodeService = new BingGeocodeService.GeocodeServiceClient("BasicHttpBinding_IGeocodeService");
                 GeocodeResponse geocodeResponse = geocodeService.Geocode(locationRequest);

                 MyWaypoint returnPoint = new MyWaypoint();
                 if (geocodeResponse.Results.Length > 0)
                 {
                     returnPoint.Latitude = geocodeResponse.Results[0].Locations[0].Latitude;
                     returnPoint.Longitude = geocodeResponse.Results[0].Locations[0].Longitude;
                     //returnPoint.Altitude = GetAltitude(returnPoint.Latitude, returnPoint.Longitude);
                     returnPoint.Altitude = 0.0;
                 }

                 return returnPoint;
             } */

        // convert object location to lat/lon
        public MyWaypoint Geocode(IAgStkObject stkObject)
        {
            IAgDataPrvFixed dataprov = stkObject.DataProviders["LLA State"] as IAgDataPrvFixed;
            IAgDrResult result = dataprov.Exec();

            Array lat = result.DataSets.GetDataSetByName("Lat").GetValues();
            Array lon = result.DataSets.GetDataSetByName("Lon").GetValues();

            MyWaypoint returnPoint = new MyWaypoint();

            returnPoint.Latitude = (double)lat.GetValue(0);
            returnPoint.Longitude = (double)lon.GetValue(0);
            returnPoint.Altitude = 0.0;

            
            return returnPoint;
        }

        // UPDATED (old method is left commented out below updated method) 
        // create route between 2 waypoints using Bing maps
        public List<MyWaypoint> CreateRoute(MyWaypoint startPoint, MyWaypoint endPoint)
        {
            //MessageBox.Show("CreateRoute method called!");// FOR DEBUGGING PURPOSES
            // create route from waypointString
            RouteRequest routeRequest = new RouteRequest()
            {
                // create Start and End SimpleWaypoints by passing in lat/lon of startPoint and endPoint
                Waypoints = new List<SimpleWaypoint>() {
                    new SimpleWaypoint(startPoint.Latitude, startPoint.Longitude),
                    new SimpleWaypoint(endPoint.Latitude, endPoint.Longitude)
                },
                BingMapsKey = m_bingMapKey
            };

            // define response (we want Route Path)
            routeRequest.RouteOptions = new RouteOptions();
            routeRequest.RouteOptions.RouteAttributes = new List<RouteAttributeType>() { RouteAttributeType.RoutePath };
            routeRequest.RouteOptions.Optimize = RouteOptimizationType.Distance;

            // create Route request
            //MessageBox.Show("Executing route request!");
            var routeResponse = routeRequest.Execute().Result;
            //MessageBox.Show("Executed route request!");

            // show message if route not returned? 
            if (routeResponse.ResourceSets.Length <= 0)
            {
                System.Console.WriteLine("No Path Found");
                return null;
            }

            // pull out the lat/lon values
            List<MyWaypoint> returnPoints = new List<MyWaypoint>();
            var route = routeResponse.ResourceSets[0].Resources[0] as Route;

            //   var result = routeResponse.ResourceSets[0].Resources as BingMapsRESTToolkit.Location[];
            //MessageBox.Show("Distance: " + routeResponse.Result.Summary.Distance.ToString()
            //    + " Time: " + routeResponse.Result.Summary.TimeInSeconds.ToString());
            
            // define and add points to returnPoints
            foreach (var point in route.RoutePath.Line.Coordinates)
            {
                MyWaypoint myWaypoint = new MyWaypoint();

                myWaypoint.Latitude = point[0];
                myWaypoint.Longitude = point[1];
                //thisPoint.Altitude = GetAltitude(thisPoint.Latitude, thisPoint.Longitude);
                myWaypoint.Altitude = 0.0;

                returnPoints.Add(myWaypoint);
            }
            //MessageBox.Show("CreateRoute method finished!"); //FOR DEBUGGING PURPOSES
            return returnPoints;
        }
        // old method 
        /*    public List<MyWaypoint> CreateRoute(MyWaypoint startPoint, MyWaypoint endPoint)
            {
                // create route from waypointString
                RouteRequest routeRequest = new RouteRequest();

                // Set the credentials using a valid Bing Maps key
                routeRequest.Credentials = new BingRouteService.Credentials();
                routeRequest.Credentials.ApplicationId = m_bingMapKey;

                // tell them that we want points along the route
                routeRequest.Options = new RouteOptions();
                routeRequest.Options.RoutePathType = RoutePathType.Points;

                //Parse user data to create array of waypoints
                BingRouteService.Waypoint[] waypoints = new BingRouteService.Waypoint[2];

                BingRouteService.Waypoint point1 = new BingRouteService.Waypoint();
                BingRouteService.Location location1 = new BingRouteService.Location();
                location1.Latitude = startPoint.Latitude;
                location1.Longitude = startPoint.Longitude;
                point1.Location = location1;
                point1.Description = "Start";
                waypoints[0] = point1;

                BingRouteService.Waypoint point2 = new BingRouteService.Waypoint();
                BingRouteService.Location location2 = new BingRouteService.Location();
                location2.Latitude = endPoint.Latitude;
                location2.Longitude = endPoint.Longitude;
                point2.Location = location2;
                point2.Description = "End";
                waypoints[1] = point2;

                routeRequest.Waypoints = waypoints;

                // Make the calculate route request
                RouteServiceClient routeService = new RouteServiceClient("BasicHttpBinding_IRouteService");
                RouteResponse routeResponse = routeService.CalculateRoute(routeRequest);

                // pull out the lat/lon values
                List<MyWaypoint> returnPoints = new List<MyWaypoint>();
                if (routeResponse.Result.Legs.Length > 0)
                {
                    //MessageBox.Show("Distance: " + routeResponse.Result.Summary.Distance.ToString()
                    //    + " Time: " + routeResponse.Result.Summary.TimeInSeconds.ToString());
                    foreach (BingRouteService.Location thisPt in routeResponse.Result.RoutePath.Points)
                    {
                        MyWaypoint thisPoint = new MyWaypoint();

                        thisPoint.Latitude = thisPt.Latitude;
                        thisPoint.Longitude = thisPt.Longitude;
                        //thisPoint.Altitude = GetAltitude(thisPoint.Latitude, thisPoint.Longitude);
                        thisPoint.Altitude = 0.0;

                        returnPoints.Add(thisPoint);
                    }
                }

                return returnPoints;
            } */

        public void PopulateGvRoute(string gvName, List<Directions.MyWaypoint> routePoints, 
            double speedValue, string speedUnits, bool useTerrain)
        {
            //MessageBox.Show($"Populate GvRoute method called! Name is {gvName}");// FOR DEBUGGING PURPOSES
            double turnRadius = 15.0;    // meter
            double granularity = 100;   // meter

            switch (speedUnits)
            {
                case "km/h":
                    m_root.UnitPreferences.SetCurrentUnit("Distance", "km");
                    m_root.UnitPreferences.SetCurrentUnit("Time", "hr");
                    turnRadius /= 1000.0;
                    granularity /= 1000.0;
                    break;
                case "mph":
                    m_root.UnitPreferences.SetCurrentUnit("Distance", "mi");
                    m_root.UnitPreferences.SetCurrentUnit("Time", "hr");
                    turnRadius /= 1609.44;
                    granularity /= 1609.44;
                    break;
                case "m/s":
                    m_root.UnitPreferences.SetCurrentUnit("Distance", "m");
                    m_root.UnitPreferences.SetCurrentUnit("Time", "sec");
                    break;
            }


            IAgStkObject gvObject = m_root.CurrentScenario.Children.New(AgESTKObjectType.eGroundVehicle, gvName);
            IAgGroundVehicle gv = gvObject as IAgGroundVehicle;
            gv.Graphics.WaypointMarker.IsWaypointMarkersVisible = false;
            //gv.Graphics.WaypointMarker.IsTurnMarkersVisible = false;
            IAgVOModel gvModel = gv.VO.Model;
            gvModel.ModelType = AgEModelType.eModelFile;
            IAgVOModelFile modelFile = gvModel.ModelData as IAgVOModelFile;
            
            if (File.Exists(installDir + @"Plugins\GreatArcPlugin\Model\mercslk.mdl" ))
            {
                modelFile.Filename = installDir + @"Plugins\GreatArcPlugin\Model\mercslk.mdl";  
            }

            gv.VO.Route.InheritTrackDataFrom2D = true;

            gv.SetRouteType(AgEVePropagatorType.ePropagatorGreatArc);
            IAgVePropagatorGreatArc prop = gv.Route as IAgVePropagatorGreatArc;

            foreach (Directions.MyWaypoint thisPt in routePoints)
            {
                IAgVeWaypointsElement thisVeWaypoint = prop.Waypoints.Add();
                thisVeWaypoint.Latitude = thisPt.Latitude;
                thisVeWaypoint.Longitude = thisPt.Longitude;
                thisVeWaypoint.Altitude = thisPt.Altitude;

                thisVeWaypoint.Speed = speedValue;
                thisVeWaypoint.TurnRadius = turnRadius;
            }

            if (useTerrain)
            {
                prop.SetAltitudeRefType(AgEVeAltitudeRef.eWayPtAltRefTerrain);
                IAgVeWayPtAltitudeRefTerrain altRef = prop.AltitudeRef as IAgVeWayPtAltitudeRefTerrain;
                altRef.Granularity = granularity;
                altRef.InterpMethod = AgEVeWayPtInterpMethod.eWayPtTerrainHeight;
            }

            prop.Propagate();
            //MessageBox.Show("GV route propagated!");// FOR DEBUGGING PURPOSES
        }
    }
}
