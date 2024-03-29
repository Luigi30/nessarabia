#include "stdafx.h"

#include "memory.h"
#include "state.h"
#include <iostream>
#include <fstream>

MemoryMap* MemoryMap::m_pInstance = NULL;

MemoryMap* MemoryMap::Instance() {
	if (!m_pInstance)
		m_pInstance = new MemoryMap();

	return m_pInstance;
}

/* These functions are a nightmare that need to be cleaned up. */

//Read a memory address.
uint8_t MemoryMap::read_byte(uint16_t address) {
	WideAddress wideAddress = uintAddressToWideAddress(address);
	return read_byte(wideAddress, true);
}

uint8_t MemoryMap::read_byte(WideAddress address, bool overrides) {
	uint8_t data = pages[address.high].read_byte(address.low);
	if (overrides) {
		handleReadOverrides(address);
	}
	return data;
}

//Write a byte to a memory address.
void MemoryMap::write_byte(uint16_t address, uint8_t data) {
	WideAddress wideAddress = uintAddressToWideAddress(address);
	write_byte(wideAddress, data);
}

void MemoryMap::write_byte(WideAddress address, uint8_t data) {
	MemoryMap::write_byte(address, data, true);
}

void MemoryMap::write_byte(WideAddress address, uint8_t data, bool overrides) {
	pages[address.high].write_byte(address.low, data);
	
	/*
	if (address >= 0x2000 && address <= 0x2007 || address == 0x4014) {
		EmulatorState::Instance()->ppuUpdatedCallback();
	}
	*/
	
	if (overrides) {
		handleWriteOverrides(address);
	}
}

//Convenience: Read a zero-page address.
uint8_t MemoryMap::read_zero_page_byte(uint8_t address) {
	WideAddress wideAddress = uintAddressToWideAddress(address);
	return pages[0].read_byte(wideAddress.low);
}

//Convenience: Write a byte to a zero-page address.
void MemoryMap::write_zero_page_byte(uint8_t address, uint8_t data) {
	WideAddress wideAddress = uintAddressToWideAddress(address);
	pages[0].write_byte(wideAddress.low, data);
}

void MemoryMap::initialize() {
	for (int i = 0; i < 255; i++) {
		pages[i] = Page();
	}
}

void MemoryMap::load_binary(uint16_t destination, std::string filename) {
	std::ifstream binaryFile;
	binaryFile.open(filename, std::ios::in|std::ios::binary);
	if (binaryFile.is_open()) {
		int x = 0;
		char data[2];

		while (!binaryFile.eof()) {
			x++;
			binaryFile.read(data, 1);
			MemoryMap::write_byte(destination, data[0]);
			destination = destination + 1;
		}

		MemoryMap::write_byte(destination - 1, '\0');

		binaryFile.close();
	}
	else {
		throw errno;
	}

}

void MemoryMap::load_binary_data(const char* data, int32_t length, uint16_t destination) {

	uint16_t runningAddress = destination;

	for (int i=0; i < length; i++) {
		MemoryMap::write_byte(runningAddress, data[i]);
		runningAddress++;
	}

}

//Convert an integer address into a wide address struct.
WideAddress uintAddressToWideAddress(uint16_t address) {
	return WideAddress{ uint8_t((address >> 8) & 0xFF), uint8_t(address & 0xFF) };
}

uint16_t wideAddressToUintAddress(WideAddress address) {
	return address.low | (address.high << 8);
}

/* These are done in hardware on the real machine. */
void MemoryMap::handleReadOverrides(WideAddress address) { }

void MemoryMap::handleWriteOverrides(WideAddress address) { }