# WandererTweaksModule

Mount & Blade II: Bannerlord mod that tweaks wanderer (companion) spawns and fixes the misleading "last seen at" text in pedia about not spawned wanderers.

Tweaks:

* Increase wanderer creation rate.
As you may know, the game creates one random wanderer every 6 in-game weeks.
As you may also know, in-game year has 12 weeks so you can kind of say that a companion is created every half-[in-game]-year.
That seems to be a bit too much so this mod changes spawn rate to X per week (per in-game month), where X currently is 2.

* Decrease settlement spawn cooldown.
You might not know this, but the game spawns a random wanderer from the list of created wanderers every time you enter a settlement.
In other words, newly created wanderers do not physically exist in the world until you eneter a settlement.
Wanderers whose culture matches settlement's culture have better chance at spawning.
After spawn the settlement is put on cooldown for 6 weeks as well and no other wanderer can spawn there during that time.
I think it is quite possible to visit all the settlements of a particular culture in way less than 6 weeks so this mod decreases cooldown to two weeks.

* Remove X least recently created "unemployed" wanderers every week.
This is done in order to balance increased wanderer creation rate and prevent your save from filling up with billions of useless wanderers, yet give you a chance to meet a specific wanderer you're looking for in a reasonable timeframe.

Spawn rate increase kicks in when your clan reaches tier 2.

Removal of wanderers kicks in when total number of available wanderers becomes greater than initial number of wanderers plus total number of companion templates.

Technically wanderers are removed using KillCharacterAction with "reason"=Lost, i.e. they will be displayed as "dead" in pedia and the text will say something like "$wanderer disappeared in $year and was reputed to be XXXX".

As for the "last seen at" fix, this will now become "last seen **near**" to give you a rough idea that the guy is not quite in town.
Technically it's possible to change this to rightful "never seen before" (that's what you see for a short while with a freshly created wanderer), but it would require a more intrusive (and thus less robust) fix.
I think this one is good enough.

# Installation

Git clone, build (see below), run the game launcher and enable the module.

# Usage

Roam around and see wanderers come and go after some ramp-up time.

# Development

Works fine with VS2019 Community Edition.

VSCode + .NET Core SDK + `dotnet build` @ powershell should be fine too.

Copy `env.example.xml` to `env.xml` and edit the settings according to your environment. Watch out for the ampersand in XML files.

The `PostBuild.ps1` script will auto execute on successful builds, and assemble the final distributable folder of the module inside the `.\dist` directory as well as install it to the game dir.

To build using CLI:
```ps1
PS C:\path-to-src\> dotnet build -c Debug # or Release
```

# Credits

Project structure inspired by https://github.com/haggen/bannerlord-module-template & https://github.com/Tyler-IN/MnB2-Bannerlord-CommunityPatch.

Thanks to **Chinesebut** for the great initial analysis in [this](https://forums.taleworlds.com/index.php?threads/adding-companions-wanderers-research-and-development.406014/post-9308561) forum post!

# Legal

© 2020 alejandro-y

This modification is not created by, affiliated with or sponsored by TaleWorlds Entertainment or its affiliates. The Mount & Blade II Bannerlord API and related logos are intelectual property of TaleWorlds Entertainment. All rights reserved.
