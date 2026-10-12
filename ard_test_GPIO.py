import Jetson.GPIO as GPIO
import time
import serial
import threading

# # Check current value1
# sudo devmem2 0x02430070

# # Write value
# sudo devmem2 0x02430070 w 0x400

# sudo devmem2 0x02434080
# sudo devmem2 0x02434080 w 0x004

# Define the GPIO pins
stopper_pin = 32  # GPIO pin for the stopper (adjust to the actual pin number)
photo_sensor_pin = 19  # GPIO pin for the photosensor (adjust to the actual pin number)
photo_sensor_tray = 33

buffer = bytearray()
start_receiving = False

# Set up GPIO
GPIO.setmode(GPIO.BOARD)
GPIO.setup(stopper_pin, GPIO.OUT, initial=GPIO.LOW)
GPIO.setup(photo_sensor_pin, GPIO.IN)
GPIO.setup(photo_sensor_tray, GPIO.IN)

# Define the protocol bytes
protocol_StopperON = bytes([0x96, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x7F, 0x7F])
#protocol_StopperON = bytes([0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x7F, 0x7F])
protocol_StopperOFF = bytes([0x96, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x8C, 0x8C])
protocol_PhtoCheck = bytes([0x96, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x6F, 0x6F])
protocol_PhtoON = bytes([0x96, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x5F, 0x5F])
protocol_PhtoOFF = bytes([0x96, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x4F, 0x4F])

# Tray Check Protocols
protocol_TrayCheck = bytes([0x96, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x3F, 0x3F])
protocol_TrayON = bytes([0x96, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x2F, 0x2F])
protocol_TrayOFF = bytes([0x96, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x1F, 0x1F])
# Function to check photosensor status
def check_photosensor_status():
    return GPIO.input(photo_sensor_pin)
def check_phototray_status():
    return GPIO.input(photo_sensor_tray)
# Initialize serial communication
ser = serial.Serial('/dev/ttyTHS1',115200)  # Adjust port and baud rate as needed
flag_tray = True
#flag_tray = check_phototray_status()
Stopper = True #
# Main loop
try:
    while ser.isOpen():
        # if flag_tray:
        #     if not check_phototray_status():
        #         print("Tray Disappear")
        #         Stopper = True
        #         GPIO.output(stopper_pin, GPIO.LOW)
        #         flag_tray = False
        # else:
        #     if check_phototray_status():
        #         print("Tray Appear")
        #         Stopper = False
        #         GPIO.output(stopper_pin, GPIO.LOW)
        #         flag_tray = True
        # Simulate receiving data (replace this with actual data receiving logic)
        incoming_byte = ser.read(1)
        incoming_byte = bytes(incoming_byte)
        #print(incoming_byte)
        if not start_receiving and incoming_byte == b'\x96':
            start_receiving = True
            buf_len = 0
        if start_receiving:
            buffer.append(incoming_byte[0])
            buf_len += 1
        if start_receiving and buf_len == 11:
            start_receiving = False
        # Check if the received data matches the protocols
            if buffer == protocol_PhtoCheck:
            # Check the photosensor status and send the corresponding protocol
                photosensor_state = check_photosensor_status()
                if photosensor_state:
                    # Implement sending protocol_PhtoON
                    ser.write(protocol_PhtoON)
                    print("Sent PhtoON protocol")
                else:
                    ser.write(protocol_PhtoOFF)
                    print("Sent PhtoOFF protocol")
            elif buffer == protocol_StopperON:
                time.sleep(0.5)
                GPIO.output(stopper_pin, GPIO.HIGH)
                print("Stopper ON")
                time.sleep(10)
                # time.sleep(60)
                GPIO.output(stopper_pin, GPIO.LOW)
            elif buffer == protocol_TrayCheck:
                Tray_state = check_phototray_status()
                if Tray_state:
                    ser.write(protocol_TrayON)
                    print("Sent TrayON protocol")
                else:
                    ser.write(protocol_TrayOFF)
                    print("Sent TrayOFF protocol")
            buffer = bytearray()

except KeyboardInterrupt:
    GPIO.cleanup()  # Clean up GPIO settings on Ctrl+C
    ser.close()  # Close the serial port on Ctrl+C
