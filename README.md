# csharpbellaworker

personal learning project - prototype bella render worker, use in conjunction with csharpbelladispatcher


Build

Get dotnet runtime

https://dotnet.microsoft.com/en-us/download

git clone this repo

BellaRender is a commercial path tracer. The c++ SDK is available but the C# SDK, while actively used in the Rhino Plugin is not a straight download. Instead we need to download the latest Rhino Bella for 6,7,8

https://bellarender.com/builds/

drag bella_rhino.macrhi file next to folder created by git clone and rename the file bella_rhino.zip

Double click on bella_rhino.zip which will unzip an create a folder named bella_rhino.rhp with bella's shared libs


from a shell/terminal inside this repo (add path to butter.bsz if needed)
```
dotnet run 
```