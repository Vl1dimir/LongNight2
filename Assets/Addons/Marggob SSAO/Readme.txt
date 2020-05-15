About

  MarggobSSAO - a post-effect that simulates Ambient Occlusion.
	
Description

  MarggobSSAO is designed according to the original scheme. 
  At a speed acceptable for modern GPUs, our SSAO provides high quality and clarity of details.
	
Features

  * Based on Raymarching method
  * Uses temporal denoising to improve the image

Limitations

  * Only Deferred Rendering
  * Guaranteed to work only on Windows
  * Dont suport URP or HDRP (In the development)

Supported Platforms

  * Windows only

Minimum Requirements

  Software

  * Unity 2018+
  * .NET 4.X equivalent

Quick Guide
  
  1) Select and apply "Image Effects/MargGob SSAO" to your main camera.
  2) Adjust the Power and Scale.

Custom properties:
  1) Create custom properties in "Project" window from "Create -> Marggob -> SSAO properties"
  2) Set your Custom Properties to "Profiles List" in ImageEffect.
  3) Chenge Profiles from your custom sripts. See sample in DemoScene.
  
Feedback

  To file error reports, questions or suggestions, you may 
  write to mail with a mark "MargGobSSAO":
	
    marggob@mail.ru