const refreshTime = (60 * 1000) / 3;
var map, watchId, userPin, directionsManager, routePath;

function GetMap() {
    map = new Microsoft.Maps.Map('#myMap', {});

    //Load the directions and spatial math modules.
    Microsoft.Maps.loadModule(['Microsoft.Maps.Directions', 'Microsoft.Maps.SpatialMath'], function () {
        //Create an instance of the directions manager.
        directionsManager = new Microsoft.Maps.Directions.DirectionsManager(map);

        //Define direciton options that you want to use, that won't be reset the next time a route is calculated.

        //Set the request options that avoid highways and uses kilometers.
        directionsManager.setRequestOptions({
            distanceUnit: Microsoft.Maps.Directions.DistanceUnit.km,
            routeAvoidance: [Microsoft.Maps.Directions.RouteAvoidance.avoidLimitedAccessHighway]
        });

        //Make the route line thick and green.
        directionsManager.setRenderOptions({
            drivingPolylineOptions: {
                strokeColor: 'green',
                strokeThickness: 6
            }
        });

        Microsoft.Maps.Events.addHandler(directionsManager, 'directionsUpdated', directionsUpdated);

        startCallWebhook();
        setInterval(function(){
            // do something only the first time the map is loaded
            startCallWebhook();
        }, refreshTime);
    });
}

function startCallWebhook()
{
    const urlSearchParams = new URLSearchParams(window.location.search);
    const params = Object.fromEntries(urlSearchParams.entries());

    let parameters = {
        "CompanyId": "Company",
        "Id": params.key
    }

    $.ajax({
        url:"../Webhooks/Company/7a204452-4271-4b0f-8358-0e49f7d65b9d",
        type:"POST",
        data:JSON.stringify(parameters),
        contentType:"application/json; charset=utf-8",
        dataType:"json",
        success: function(data){
            processWebhook(data);
        }
    });
}

function processWebhook(data)
{
    startTracking(data);
}

function startTracking(data) {
    //Add a pushpin to show the user's location.
    if(!userPin) {
        userPin = new Microsoft.Maps.Pushpin(map.getCenter(), { visible: false });
        map.entities.push(userPin);
    }

    //Watch the users location.
    usersLocationUpdated(data.LatestLocation);
}

function usersLocationUpdated(position) {
    var loc = new Microsoft.Maps.Location(
        position.latitude,
        position.longitude);

    //Update the user pushpin.
    userPin.setLocation(loc);
    userPin.setOptions({ visible: true });

    //Center the map on the user's location.
    map.setView({ center: loc });

    //Calculate a new route if one hasn't been calculated or if the users current location is further than 50 meters from the current route.
    if (!routePath || Microsoft.Maps.SpatialMath.distance(loc, routePath) > 50) {
        calculateRoute(loc, null);
    }
}

function stopTracking() {
    // Cancel the geolocation updates.
    navigator.geolocation.clearWatch(watchId);

    //Remove the user pushpin.
    map.entities.clear();
    clearDirections();
}

function calculateRoute(userLocation, destination) {
    clearDirections();

    //Create waypoints to route between.
    directionsManager.addWaypoint(new Microsoft.Maps.Directions.Waypoint({ location: userLocation }));
    directionsManager.addWaypoint(new Microsoft.Maps.Directions.Waypoint({ address: "Word trade center, montecito 38, cdmx, mx" }));     

    //Calculate directions.
    directionsManager.calculateDirections();
}

function directionsUpdated(e) {
    //When the directions are updated, get a polyline for the route path to perform calculations against in the future.
    var route = directionsManager.getCurrentRoute();

    if (route && route.routePath && route.routePath.length > 0) {
        routePath = new Microsoft.Maps.Polyline(route.routePath);
    }
}

function clearDirections() {
    //Clear directions waypoints and display without clearing it's options.
    directionsManager.clearDisplay();

    var wp = directionsManager.getAllWaypoints();
    if (wp && wp.length > 0) {
        for (var i = wp.length - 1; i >= 0; i--) {
            this.directionsManager.removeWaypoint(wp[i]);
        }
    }

    routePath = null;
}