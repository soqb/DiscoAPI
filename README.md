# A Disco Elysium Modding API
Well I'll be. If there's ever been a game so deserving of a vibrant modding community and yet so frowned upon by the benevolent kings of isola at Bethesda and the noble craftsmen of fabric, forge and quilt, I think I'll eat my hat.

This project is an *absolute fledgling* and it's going to take Big Work™ to grow a mature and flexible API.
Please help if you can.

# Installation

- Ensure you have the [.NET SDK 6.0](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) installed.
- Please follow [The BepInEx installation instructions (Il2Cpp version)](https://docs.bepinex.dev/v6.0.0-pre.1/articles/user_guide/installation/index.html) and ensure that you have run the game with it at least once.
- Clone this repository.
- Copy `doc/config.targets.template` to `config.targets` and edit as necessary.
- Build the project with `dotnet build`.
  * This will automatically copy the compiled plugin to the correct folder, overwriting any old version present.
- Launch the game!

# Examples
- The best example the project provides is found [here](src/Runtime/Examples/Transgener.cs).
