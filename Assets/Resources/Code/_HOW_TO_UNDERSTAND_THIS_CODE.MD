All code is split up into a few categories that have folders



Controllers

* These are things we drag and drop onto the player that control stuff like player movement, making stuff highlight when the mouse is over it, etc. They have lots of logic and hold complicated data.
* Someday you'd make a SpaceshipController and drag and drop that onto a spaceship prefab.
* Controllers should NOT reach into other controllers and change their state. That's what mediators are for. But, controllers can look at the state of other controllers, or pass them around
* It's not a super strict pattern, but in general, the less controller know about each other, the better. We don't want to have 8 controllers on the player and have them all hard coded to need each other.



Mediators

* Mediators are for when you have some need for one controller to reach into another controller. Instead of doing that, you have a mediator that reaches into two controllers.
* That way the controllers dont have to know each other exist to have some complex interaction.
* EG, when we want to go from walking around to playing a mini game of installing a bolt, we need to tell the input controller to switch input to use the keys for mini games and tell fps controller to show the cursor again
* Neither of those are really the jobs of the input controller or the fps controller to coordinate. we need some manager above them to do it



Components

* These are things that you put on prefabs like bolts or bolt slots. They have logic and state that controllers will go use.
* Like, the Player interaction controller will call OnInteract on a bolt, and the bolt component code keep track of whether that bolt is installed or what kind of tool you need to interact with it etc.



Utilities

* These are just useful bits of code that are static so they can be called from anywhere.



MiniGames

* This is where the logic for how stuff like "you unscrew a bolt by dragging the mouse into the sweet spot" etc lives.



If you are ever confused, use debug mode in visual studio! Ask AI how to install Visual Studio for Unity. Go into code and set a break point. Press "attach to unity". You can literally see line by line how the code executes, see it go into each file as it is needed etc. There is no better way to understand how code works.

