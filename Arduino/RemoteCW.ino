//Remote CW
unsigned long nextTime = 0;
const int digitalLeftPin = 10;
const int digitalRightPin = 11;
bool lastLeftPin = false;
bool lastRightPin = false;

void setup() {
  Serial.begin(115200);
  pinMode(digitalLeftPin, INPUT_PULLUP);
  pinMode(digitalRightPin, INPUT_PULLUP);
}

void loop() {
  
  unsigned long newTime = millis();
  if (newTime > nextTime)
  {
    bool leftPin = !digitalRead(digitalLeftPin);
    bool rightPin = !digitalRead(digitalRightPin);
    nextTime = newTime + 1;
    
    if (leftPin != lastLeftPin)
    {
      Serial.write("L ");
      //Serial.write(String(nextTime).c_str());
      String stateText = "0";
      if (leftPin)
      {
        stateText = "1";
      }
      Serial.write(stateText.c_str());
      Serial.write("\n");
      lastLeftPin = leftPin;      
    }
    
    if (rightPin != lastRightPin)
    {
      Serial.write("R ");
      //Serial.write(String(nextTime).c_str());
      String stateText = "0";
      if (rightPin)
      {
        stateText = "1";
      }
      Serial.write(stateText.c_str());
      Serial.write("\n");
      lastRightPin = rightPin;      
    }
  }
}
