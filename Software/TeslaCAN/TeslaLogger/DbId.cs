// 
//  TeslaCAN
// 
//  Copyright 2020 Martin Gerczuk
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

namespace TeslaCAN.TeslaLogger
{
    public enum DbId
    {
        CellTempMin = 1,
        CellTempMid = 2,
        CellTempMax = 3,
        CellTempDiff = 4,
        CellVoltMin = 5,
        CellVoltMid = 6,
        CellVoltMax = 7,
        CellDiff = 8,
        AC_ChargeTotal = 9,
        DC_ChargeTotal = 11,
        ChargeTotal = 13,
        RegenTotal = 16,
        DischargeTotal = 20,
        SOC = 23,
        SOC_UI = 24,
        SOC_Min = 25,
        Odometer = 26,
        CellImbalance = 27,
        BMSMaxCharge = 28,
        MaxChargePower = 28,
        BMSMaxDischarge = 29,
        MaxDischargePower = 29,
        BatteryVoltage = 30,
        BatteryHeaterTemp = 31,
        _12vSystems = 32,
        DC_DCCoolantInlet = 33,
        DC_DCCurrent = 34,
        DC_DCEfficiency = 35,
        DC_DCVoltage = 36,
        DC_DCInputPower = 37,
        DC_DCOutputPower = 38,
        _400VSystems = 39,
        TypicalRange = 40,
        HeaterLeft = 41,
        HeaterRight = 42,
        BatteryPower = 43,
        BatteryCurrent = 44,

        //HVAC on/off = 45,
        //HVAC A/C = 46,
        //HVAC fan speed = 47,
        //HVAC window = 48,
        //HVAC temp left = 49,
        //HVAC temp right = 50,
        //HVAC mid = 51,
        //HVAC floor = 52,
        //Mid vent L = 53,
        //Floor vent L = 54,
        //Mid vent R = 55,
        //Floor vent R = 56,
        //Chiller bypass = 57,
        //Inside temp = 58,
        OutsideTempFiltered = 59,

        //A/C air temp = 60,
        OutsideTemp = 61,

        //Full typical range = 62,
        //Full rated range = 63,
        BatteryInlet = 64,

        //Rated range = 65,
        //Odometer(legacy) = 66,
        //Battery pump 1 = 67,
        //Battery pump 2 = 68,
        RadiatorBypass = 69,

        //Refrigerant temp = 70,
        NominalFullPack = 71,
        NominalRemaining = 72,

        //Usable full pack = 73,
        ExpectedRemaining = 74,
        IdealRemaining = 75,

        //Discharge cycles = 76,
        //Series/Parallel = 77,
        //Powertrain pump = 78,
        //To charge complete = 79,
        PTInlet = 80,

        //Powertrain inlet = 80,
        //PTC air heater = 81,
        //Coolant heater = 82,
        //Thermal controller 400V = 83,
        //Thermal controller = 84,
        //Thermal controller 12V = 85,
        //Usable remaining = 86,
        EnergyBuffer = 87,

        FTorque = 400,

        //Rr/Fr torque bias = 401,
        RTorque = 403,
        AcceleratorPedal = 404,
        FPower = 405,

        //Fr dissipation = 406,
        //Fr input power = 407,
        //Fr mech power HP = 408,
        //Fr stator current = 409,
        //Fr drive power max = 410,
        //Mech power combined = 411,
        //HP combined = 412,
        //Fr efficiency = 413,
        //Rr inverter 12V = 414,
        RPower = 415,

        //Rr dissipation = 416,
        //Rr input power = 417,
        //Propulsion = 418,
        //Rr mech power HP = 419,
        //Rr stator current = 420,
        //Rr regen power max = 421,
        //Rr drive power max = 422,
        //Rr efficiency = 423,
        //Fr torque estimate = 424,
        //Rr torque estimate = 425,
        Consumption = 426,

        //Rr coolant inlet = 427,
        //Rr inverter PCB = 428,
        RStatorTemp = 429,

        //Rr DC capacitor = 430,
        //Rr heat sink = 431,
        //Rr inverter = 432,
        //Fr motor RPM = 433,
        //Rr motor RPM = 434,
        //Brake pedal = 435,
        //Front left = 436,
        //Front right = 437,
        //Front drive ratio = 438,
        //Rear left = 439,
        //Rear right = 440,
        //Rear drive ratio = 441,
        Speed = 442,
        FStatorTemp = 443,
        PowertrainFlow = 444,
        BatteryFlow = 445
    }
}