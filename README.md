# Tranzmit
Unity 3D Event system that uses the new Experimental Unity GraphView (https://docs.unity3d.com/ScriptReference/Experimental.GraphView.GraphView.html), allowing visualization of the flow of event data.

REQUIRED COMPONENT - Will Not Compile without Odin Inspector!:
https://odininspector.com/

NOTE - Graph will not be operational until an Event has been added.

### Details
I love Event Driven Architectures (EDA). It makes for nice, decoupled code. But I am not a fan of how ambiguous the relationships between scripts can be. Enter Tranzmit, a fully functional, object based (send anything!) event system... with a twist!

### Main Features:
* Multi level error checking
* Real time visual feedback
* Various code examples
* Send any object
* Minimal coding required
* Clean Interface power by Odin Inspector
* Multi platform
* Zero Garbage Collection when ran as compiled EXE (as far as my tests show!)

Leveraging the new, and still experimental Unity GraphView API, along with the power of Odin Inspector, you can also see in real time which scripts are sending events, and which scripts are listening, receiving ...and failing! Clicking on graph elements takes you to the scripts, acting as a navigation system for your code!

Why the dependancy on Odin Inspector? In short, this project was coded for myself, and the last thing I want to be doing is hand coding custom Unity interfaces, when I can do it in a fraction of the time (and better!) with Odin Insector. If you do any kind of coding in Unity on a regular basis, you should seriously consider using Odin Inspector. It's the first thing I setup in any new project.

That said, if there is enough interest in Tranzmit without Odin Inspector, I would consider developing the custom interfaces required.

I have no affiliation with the amazing people at Sirenix (the makers of Odin Inspector).

I hope this proves useful to someone, and if you have any comments or improvements regarding this code, please do so! :) 

Additional info will be made available at https://blep.io

Overview video on YouTube: https://youtu.be/BfAUmtgjHac

![Image of Trazmit Components](https://blep.io/wp-content/uploads/2020/07/Trazmit-Main.png)
![Image of Trazmit GraphView](https://blep.io/wp-content/uploads/2020/07/Trazmit-Graph.png)
