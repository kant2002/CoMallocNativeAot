Example how to implement IMallocSpy in NativeAOT
================================================

In this example, provided sample how to implemet ComWrappers which provide implementation for the [IMallocSpy](https://docs.microsoft.com/en-us/windows/win32/api/objidl/nn-objidl-imallocspy)

Run following in x64 Native Tools for Visual Studio 2019

	cd CoMallocNativeAot
	dotnet publish -c Release -r win-x64
	bin\x64\Release\net5.0-windows\win-x64\publish\CoMallocNativeAot.exe
