<div id="top"></div>

[![Contributors][contributors-shield]][contributors-url]
[![Forks][forks-shield]][forks-url]
[![Stargazers][stars-shield]][stars-url]
[![Issues][issues-shield]][issues-url]
[![MIT License][license-shield]][license-url]


<br />
<div align="center">
  <a href="https://github.com/Jansen-w/BannerlordOverhaulFramework">
    <img src="https://www.taleworlds.com/storage/images/wallpapers/14/1920x1080/mnb-ii-b-03.jpg" alt="Logo" width="960" height="540">
  </a>

<h2 align="center">Bannerlord Overhaul Framework</h2>
  <p align="center">
    A total-overhaul framework for Mount and Blade II Bannerlord
    <br />
    <a href="https://github.com/Jansen-w/BannerlordOverhaulFramework">Getting Started</a>
    ·
    <a href="https://github.com/Jansen-w/BannerlordOverhaulFramework/issues">Report a Bug</a>
    ·
    <a href="https://github.com/Jansen-w/BannerlordOverhaulFramework/issues">Request a Feature</a>
  </p>
</div>

## What is BOF?

Bannerlord Overhaul framework is blank-canvas campaign implementation intended to be forked and customized by total-overhaul modders. BOF serves as a lightweight and easy to understand base to work off for modders wanting to change the core functionality of the campaign system with minimal usage of harmony patching and reflection.

<p>&nbsp;</p>

## Who is this framework made for?
- Total conversion mods aiming to completely revamp bannerlords content
- Modders looking to make vast changes the core functionality of Bannerlord
- Modders with a low priority on mod compatibility

<p>&nbsp;</p>

## Roadmap

- [ ] Copy all of Bannerlords campaign system into this project, decoupling as much of this implementation from Taleworlds' code as possible.
- [ ] Slim down the copied classes by removing bloated features, classes, and methods.
    - [ ] Identify critical classes and structs that may be problematic to modify
- [ ] Document the slimmed down campaign implementation and APIs
- [ ] Implement a new save/load system using SQLite

See the [open issues](https://github.com/Jansen-w/BannerlordOverhaulFramework/issues) for a full list of proposed features (and known issues).

<p>&nbsp;</p>

## Using this framework to build your mod

This framework will be designed primarily to be forked then customized to your mod's needs. The changes made to this repository will be as generic as possible, and should be expanded upon to fit your projects needs in your own repository.

<p>&nbsp;</p>

## Contributing

Contributions to this repo are welcome. Keep in mind this framework is intended to be a generic base for any type of total overhaul, so suggestions and features that are too narrowly scoped will probably not be accepted/merged.

To suggest a feature you have implemented, please fork the repo (if you haven't already), commit your changes, then create a pull request. If you just have a cool idea, you can simply open an issue with the tag "enhancement".

1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/feature-name`)
3. Commit your Changes (`git commit -m 'Add a cool feature'`)
4. Push to the Branch (`git push origin feature/feature-name`)
5. Open a Pull Request

<p>&nbsp;</p>

<!-- LICENSE -->
## License

Distributed under the MIT License. See the `LICENSE` file for more information.

<p>&nbsp;</p>

<!-- CONTACT -->
## Need help?

Feel free to reach out/ask for help in the [official Bannerlord modding discord](https://discord.gg/WgRbZFDJbQ)

Project Link: [https://github.com/Jansen-w/BannerlordOverhaulFramework](https://github.com/Bannerlord-Modding/BannerlordOverhaulFramework)

<p align="right">(<a href="#top">back to top</a>)</p>


[contributors-shield]: https://img.shields.io/github/contributors/Jansen-w/BannerlordOverhaulFramework?style=for-the-badge
[contributors-url]: https://github.com/Jansen-w/BannerlordOverhaulFramework/graphs/contributors
[forks-shield]: https://img.shields.io/github/forks/Jansen-w/BannerlordOverhaulFramework?style=for-the-badge
[forks-url]: https://github.com/Jansen-w/BannerlordOverhaulFramework/network/members
[stars-shield]: https://img.shields.io/github/stars/Jansen-w/BannerlordOverhaulFramework?style=for-the-badge
[stars-url]: https://github.com/Jansen-w/BannerlordOverhaulFramework/stargazers
[issues-shield]: https://img.shields.io/github/issues/Jansen-w/BannerlordOverhaulFramework?style=for-the-badge
[issues-url]: https://github.com/Jansen-w/BannerlordOverhaulFramework/issues
[license-shield]: https://img.shields.io/github/license/Jansen-w/BannerlordOverhaulFramework?style=for-the-badge
[license-url]: https://github.com/Jansen-w/BannerlordOverhaulFramework/blob/master/LICENSE.txt