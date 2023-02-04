import gpiod
import sys
import time
import select
import socket
import struct
import queue
import datetime

chip = gpiod.chip("/dev/gpiochip0")
cw = chip.get_line(65)

config = gpiod.line_request()
config.consumer = "CWKey"
config.request_type = gpiod.line_request.DIRECTION_OUTPUT

cw.request(config)
cw.set_value(0)

print("CW Driver Starting")
print(cw.consumer)

sock = socket.socket(socket.AF_INET6, socket.SOCK_DGRAM)
sock.bind(("2403:580b:34:20::6", 5005))
sock.setblocking(0)

sockArray = [ sock ]
emptyArray = []

nextEvent = None
nextEventTime = None 
newestEventTime = 0
eventQueue = queue.Queue()
epoch = datetime.datetime(1, 1, 1)
cwOnTime = None

def processData(data):
    global newestEventTime
    for i in range(49, -1, -1):
      u8 = data[i*9 : i*9 + 8]
      timeLong = int.from_bytes(u8)
      state = data[i * 9 + 8]
      if (timeLong > newestEventTime):
          eventQueue.put((timeLong, state))
          newestEventTime = timeLong
          #print("EVENT %d = %s" % (timeLong, state))

while True:
    rlist, wlist, xlist = select.select(sockArray, emptyArray, emptyArray, 1)
    #Receive from UDP
    if (len(rlist) > 0):
      data, addr = sock.recvfrom(1024)
      if (len(data) == 450):
          processData(data)
    #Process Queue
    if (nextEvent == None and not eventQueue.empty()):
      nextEvent = eventQueue.get()
      nextEventTime = datetime.datetime(1,1,1) + datetime.timedelta(microseconds = nextEvent[0] // 10)
    if (nextEvent != None):
        currentTime = datetime.datetime.utcnow()
        #1000ms latency buffer
        delta = ((currentTime - nextEventTime).total_seconds() * 1000) - 1000
        if (delta > 5000):
            nextEvent = None
            continue
        if (delta > -50):
            if (delta < 0):
                time.sleep(-delta / 1000)
            newState = nextEvent[1]
            cw.set_value(newState)
            currentTime = datetime.datetime.utcnow()
            if (newState):
              cwOnTime = currentTime
            else:
              cwOnTime = None
            #print("CW %s = %d" % (currentTime, newState))
            nextEvent = None

    #CW Timeout
    currentTime = datetime.datetime.utcnow()
    if (cwOnTime != None):
      if ((currentTime - cwOnTime).total_seconds() > 5):
        print("Timeout triggered at %s" % (currentTime))
        cwOnTime = None
        cw.set_value(0)
        
    if (eventQueue.empty()):
        time.sleep(0.05)
    time.sleep(0.001)

print("Done!")
