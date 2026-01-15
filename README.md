# A Disco Elysium Modding API

A work-in-progress modding framework for Disco Elysium.

# Installation

- Ensure you have the [.NET SDK 6.0](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) installed.
- Please follow [The BepInEx installation instructions (Il2Cpp version)](https://docs.bepinex.dev/v6.0.0-pre.1/articles/user_guide/installation/index.html),
  using build version be-697 which can be found [here](https://builds.bepinex.dev/projects/bepinex_be),
  and ensure that you have run the game with it at least once.
- Clone this repository.
- Copy `doc/config.targets.template` to `config.targets` and edit as necessary.
- Build the project with `dotnet build`.
  * This will automatically copy the compiled plugin to the correct folder, overwriting any old version present.
- Launch the game!

# License

This project is permissively dual-licensed under either of the following at your option:
* MIT License ([LICENSE-MIT](doc/LICENSE-MIT) or [http://opensource.org/licenses/MIT](http://opensource.org/licenses/MIT))
* Apache License, Version 2.0 ([LICENSE-APACHE](doc/LICENSE-APACHE) or [http://www.apache.org/licenses/LICENSE-2.0](http://www.apache.org/licenses/LICENSE-2.0))
