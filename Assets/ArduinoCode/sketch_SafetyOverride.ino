// FINAL FIRMWARE: LISTENER MODE
// Baud Rate: 115200

const int greenPin = 2;
const int redPin = 3;
const int potPin = A0;

unsigned long lastSendTime = 0;

void setup() {
  Serial.begin(115200);
  pinMode(greenPin, OUTPUT);
  pinMode(redPin, OUTPUT);
  
  // STARTUP SIGNAL: Rapid Flash Green-Red-Green-Red
  // If you don't see this when you upload, something is wrong.
  for(int i=0; i<3; i++) {
    digitalWrite(greenPin, HIGH); delay(100); digitalWrite(greenPin, LOW);
    digitalWrite(redPin, HIGH); delay(100); digitalWrite(redPin, LOW);
  }
}

void loop() {
  // 1. LISTEN TO UNITY
  if (Serial.available() > 0) {
    char command = Serial.read();
    
    if (command == 'G') {
      digitalWrite(greenPin, HIGH);
      digitalWrite(redPin, LOW);
    } 
    else if (command == 'R') {
      digitalWrite(greenPin, LOW);
      digitalWrite(redPin, HIGH);
    }
  }

  // 2. SEND TO UNITY (Knob)
  if (millis() - lastSendTime > 20) {
    int val = analogRead(potPin);
    Serial.println(val); 
    lastSendTime = millis();
  }
}