# DeflectionMeasure
A Small Project utilizing AForge to measure angular rotation of two selected dots on a plane.

This tool supports dynamic selection of video source. Selecting of the points of interest by color and position picker and tracking the points in a certain tolared area (relative to last position) in order to measure angular movement.
Certain steps can be saved in a formated list and copied to clipboard.

![](https://github.com/Gustice/DeflectionMeasure/blob/master/Documentation/GUI%20Preview.jpg) 

Please see Wiki for further Details.

Please note, that this is still under development.
Some features like dot-tracking seem to work properly. However, there are still some issues left especially regarding the robustness of the detection.
Also the evaluation of the system limits and the possible accuracy is pending.


## Credits
Special thanks to Burak Ozeroglu. He solved a similar case by using AForge and his code helped me to setup the back-end of my solution really fast.
