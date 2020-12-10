#include <avr/sleep.h>

int piPowerOnInd = 2;
int extPowerInd = 3;
int piReset = 4;
int piShutdownReq = 8;
int powerLed = 9;

void setup()
{
  pinMode(piPowerOnInd, INPUT);
  pinMode(extPowerInd, INPUT);

  digitalWrite(piReset, digitalRead(piPowerOnInd));
  pinMode(piReset, OUTPUT);

  digitalWrite(powerLed, digitalRead(piPowerOnInd));
  pinMode(powerLed, OUTPUT);

  digitalWrite(piShutdownReq, 1);
  pinMode(piShutdownReq, OUTPUT);

  Serial.begin(9600);
  Serial.println("Startup");
  delay(200);
    
  set_sleep_mode(SLEEP_MODE_PWR_DOWN);
  
  if (digitalRead(extPowerInd) == 0)
    switchPiOff();
  else
    switchPiOn();
}

// interrupt service routine in sleep mode
void piPowerIsr ()
{
  sleep_disable ();
  detachInterrupt (digitalPinToInterrupt (extPowerInd));
}

int waitForExtPower()
{
  noInterrupts ();
  sleep_enable ();    // enables the sleep bit in the mcucr register
  EIFR = bit (INTF1); // clear flag for interrupt 1
  attachInterrupt (digitalPinToInterrupt (extPowerInd), piPowerIsr, CHANGE);
  interrupts();
  sleep_mode ();      // here the device is actually put to sleep!!

  return digitalRead(extPowerInd);
}

void switchPiOn()
{
  Serial.println("switchPiOn");
  digitalWrite(piReset, 1);

  int ledValue = 0;
  while (digitalRead(piPowerOnInd) == 0)
  {
    delay(200);
    ledValue = !ledValue;
    digitalWrite(powerLed, ledValue);
  }
  digitalWrite(powerLed, 1);
  
  Serial.println("Pi is up!");
  delay(100);
}

void requestShutdown()
{
  digitalWrite(piShutdownReq, 0);
  delay(200);
  digitalWrite(piShutdownReq, 1);
}

void switchPiOff()
{
  Serial.println("switchPiOff");
  
  requestShutdown();
  
  int ledValue = 1;
  int count = 10;
  while (digitalRead(piPowerOnInd) == 1)
  {
    if (count-- == 0)
    {
      requestShutdown();
      count = 10;
    }
    else
      delay(200);
      
    ledValue = !ledValue;
    digitalWrite(powerLed, ledValue);

  }
  digitalWrite(powerLed, 0);
  
  Serial.println("Pi is down - reset = 0");
  digitalWrite(piReset, 0);
  delay(100);
}

void loop() 
{
  int oldPowerState;
  int extPowerState = waitForExtPower();
  do
  {
    if (extPowerState == 0)
    {
      switchPiOff();
    }
    else
    {
      switchPiOn();
    }
    oldPowerState = extPowerState;
    extPowerState = digitalRead(extPowerInd);
  } 
  while (oldPowerState != extPowerState);
}
