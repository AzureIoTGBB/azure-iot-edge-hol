#include <DHT.h>
#define DHTTYPE DHT22

//Set’s the pin we’re reading data from and initializes the sensor.
int DHTPIN = 2;
DHT dht(DHTPIN,DHTTYPE);

String inputString = "";         // a string to hold incoming data
boolean stringComplete = false;  // whether the string is complete

#define pinLED 13     // pin 13 is the onboard LED

void setup() {
  //Tell the arduino we’ll be reading data on the defined DHT pin
  pinMode(DHTPIN, INPUT);
  
  //Open the serial port for communication
  Serial.begin(9600);
  
  //start the connection for reading.
  dht.begin();

  // we will be 'writing' to the pin, vs. reading
  pinMode(pinLED, OUTPUT);
  // start with the LED off
  digitalWrite(pinLED, LOW);

}

void loop() {
  //declare variables for storing temperature and humidity and capture
  float h = dht.readHumidity();
  float t = dht.readTemperature(true);
  
  //output data as humidity,temperature
  Serial.print(h);
  Serial.print(",");
  Serial.println(t);  //println includes linefeed

    serialEvent(); //call the function to read any command in the serial buffer
  // print the string when a newline arrives:
  if (stringComplete) {
//    Serial.println(inputString);

    // turn LED on or off depending on command
    if(inputString == "OFF")
      digitalWrite(pinLED, LOW);
    if(inputString == "ON")
      digitalWrite(pinLED, HIGH);
    
    // clear the string:
    inputString = "";
    stringComplete = false;
  }
  
  //sleep for two seconds before reading again
  delay(2000);
}



void serialEvent() {
  // while there is data to read in the buffer, read it
  while (Serial.available()) {
    // get the new byte:
    char inChar = (char)Serial.read();
    // add it to the inputString.  if it's a newline, bail as that completes the message
    if (inChar == '\n') {
      stringComplete = true;
    }
    else
      inputString += inChar;
  }
}