FACECROP - Face extraction program by /u/NihilistByte
=====================================================

QUICKSTART
******************************************************
Launch facecrop.exe for the first time. Due to the absence of filecrop.ini file, the program will proceed in 
creating a default parameter file (facecrop.ini). Closing the program, you must customize the content of facecrop.exe
according to your needs.

Here follows a sample of the standard facecrop.ini:
------------------------------------------------------
SOURCE_FOLDER = c:\tmp\faces
FILE_MASK = *.*
HAAR_MINSIZE = 30
SEARCH_MODE = 3
SCALING_FACTOR = 1,2
SCALING_MODE = 0
PARALLEL_PROCESSING = True
SUPPRESSION = 3
DESTINATION_FOLDER = c:\tmp\faces\cropped
OUTPUT_PREFIX = outp
OUTPUT_TYPE = png
OUTPUT_SIZE = 250
------------------------------------------------------

SOURCE_FOLDER represent the path of the images to crop. Basically, the folder which contains frames obtained 
by ffmpeg processing.

FILE_MASK indicates the file pattern to search for in SOURCE_FOLDER. 
For example, *.png will process PNG extension only

HAAR_MINSIZE is the minimum size for faces to search for into images. A value of 30 tells the program to search
for faces no smaller than 30 pixels

SEARCH_MODE is the search mode used by Accord.NET framework to search for faces. Possible values are:
	
	0 = Default
	Entire image will be scanned

	1 = Single 
	Only a single object will be retrieved

	2 = NoOverlap
	If a object has already been detected inside an area, it will not be scanned twice for inner or 
	overlapping objects, saving computation time.

	3 = Average
	If several objects are located within one another, they will be averaged. 
	Additionally, objects which have not been detected sufficient times may be dropped by 
	setting Accord.Vision.Detection.HaarObjectDetector.Suppression.

SCALING FACTOR sets the rescaling factor to rescale the window during search 

SCALING_MODE

	0 = GreaterToSmaller
	Will start with a big search window and gradually scale into smaller ones.

	1 = SmallerToGreater
	Will start with small search windows and gradually scale into greater ones.

PARALLEL_PROCESSING 
sets a value indicating whether this Accord.Vision.Detection.HaarObjectDetector should scan the image 
using multiple threads. This setting can only be changed to true on .NET version which support the 
Parallel Tasks framework (4.0+).

SUPPRESSION
The value of this property represents the minimum amount of detections made inside a region to report this region 
as an actual detection. For example, setting this property to two will discard all regions which had not achieved 
at least two detected rectangles within it. Setting this property to a value higher than zero may decrease the 
number of false positives.

DESTINATION_FOLDER is the folder in which the cropped images will be saved

OUTPUT_PREFIX is the prefix to apply to generated images. For example, leaving the default value "outp" will produce
a set of image file named outp00001, outp00002, and so on

OUTPUT_TYPE is the type of file to produce. Accepted values are JPG, JPEG, BMP, PNG. Every other value will be
defaulted to PNG

OUTPUT_SIZE is the value to which resize the cropped image. A value of 250, for example, will produce a set of
images of 250x250 px.

HOW THE PROGRAM WORKS
******************************************************
After having specified correct / desired parameters in facecrop.ini, simply execute facecrop.exe.
The program will use the indicated parameters to process the files saved in source folder, saving the found faces
in destination folder. At the end of cropping, a last check will scan for identical files in output folder, removing
duplicates and leaving only a single copy of every found face.
The messages sent to console will be saved in facecrop.log, a file saved in the same location of the program, and
rewrited at every execution of the app.

DEMONSTRATIVE VIDEO
******************************************************
For a quick demo video, please check the following URL
https://drive.google.com/open?id=1yEp5__EnNBPX4dZzP9EsAhz0dKPHIZXz

