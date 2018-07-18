# oneM2MBrowser

oneM2MBrowser is a tool for monitoring the oneM2M resources in the Mobius Yellow Turtle([Mobius](https://github.com/IoTKETI/Mobius)). It is working base on oneM2M HTTP RESTful API and MQTT Message. It was designed for helping developer to easily work with Mobius.

## Getting Started

Download the source file from [oneM2MBrowser github site](https://github.com/IoTKETI/oneM2MBrowser). 

## Installation

The oneM2MBrowser project is a Vistual Studio project. So it only can work on window PC.

If you don't have the winPC that we suggest to insall the [VMware WorkStation Player](https://www.vmware.com/products/player/playerpro-evaluation.html) and run a Virtrual Machine on it.

1. Download the [Visual Studio](https://www.visualstudio.com/).

2. Install the Visual Studio follow the guide.

3. Open the oneM2MBrowser home and double click oneM2MBrowser.sln file.

## Running

1. Press the F5 key in Visual studio or click the start button on top of Visual studio IDE.

2. Input a resource URL into oneM2MBrowser and click start button. oneM2MBrowser will show the all child resources of yours after finishing discovery process. 

![Resource viwer](https://user-images.githubusercontent.com/29790334/27902099-7390f3f2-626f-11e7-86ac-be3405d3beb5.PNG)

## Development

The oneM2MBrowser is developed base on [WPF application](https://msdn.microsoft.com/en-us/library/mt149842(v=vs.140).aspx) which include in .net framework.

So the UI part is written with [XAML](https://msdn.microsoft.com/en-us/library/cc295302.aspx) language and logic part is written with C#.

If you are not familiar with XAML language the Microsoft also provide [Microsoft Blend](https://msdn.microsoft.com/en-us/library/jj171012.aspx) for helping developer to edit UI XAML.

## Build

### Basic framework

* [WPF](https://msdn.microsoft.com/en-us/library/mt149842(v=vs.140).aspx) - The UI framework used

### Nuget libraries

* [Newtonsoft.Json](http://www.newtonsoft.com/json) - The JSON format parser
* [M2Mqtt](https://www.nuget.org/packages/M2Mqtt/4.3.0) - The MQTT message protocol used

## Document
If you want more detail, please refer the [guide document](https://github.com/IoTKETI/oneM2MBrowser/raw/master/doc/oneM2M%20Browser%20User%20Guide_v1.2_EN.pdf).

## Authors

* **Chen Nan** - *Initial work* - [coffeenan](https://github.com/coffeenan) (xuehu0000@keti.re.kr)
* **JongGwan An** - *Update work* -  (kman3212@gmail.co.kr)

