#include "stdafx.h"
#include "api.h"
#include "state.h"
#include <string>

M6502EMULATORDLL_API bool loadBinary(const char* path, uint16_t address) {
	MemoryMap::Instance()->load_binary(address, path);
	return true;
}

M6502EMULATORDLL_API bool loadBinaryData(const char* data, int32_t size, uint16_t address) {
	MemoryMap::Instance()->load_binary_data(data, size, address);
	return true;
}

M6502EMULATORDLL_API UINT16 getProgramCounter() {
	return 0xFFFF;
}

M6502EMULATORDLL_API ProcessorStatus getProcessorStatus() {
	ProcessorStatus status = ProcessorStatus();
	status.FLAG_SIGN = EmulatorState::Instance()->processor.getSignFlag();
	status.FLAG_OVERFLOW = EmulatorState::Instance()->processor.getOverflowFlag();
	status.FLAG_BREAKPOINT = EmulatorState::Instance()->processor.getBreakpointFlag();
	status.FLAG_DECIMAL = EmulatorState::Instance()->processor.getDecimalFlag();
	status.FLAG_INTERRUPT = EmulatorState::Instance()->processor.getInterruptFlag();
	status.FLAG_ZERO = EmulatorState::Instance()->processor.getZeroFlag();
	status.FLAG_CARRY = EmulatorState::Instance()->processor.getCarryFlag();

	status.accumulator = EmulatorState::Instance()->processor.getAccumulator();
	status.index_x = EmulatorState::Instance()->processor.getIndexX();
	status.index_y = EmulatorState::Instance()->processor.getIndexY();
	status.stack_pointer = EmulatorState::Instance()->processor.getStackPointer();
	status.program_counter = EmulatorState::Instance()->processor.getProgramCounter();
	status.old_program_counter = EmulatorState::Instance()->processor.getOldProgramCounter();
	status.cycles = EmulatorState::Instance()->processor.getCycleCount();
	status.last_opcode = EmulatorState::Instance()->processor.last_executed_opcode.c_str();
	//OutputDebugString(L"getProcessorStatus()\r\n");
	return status;
}

M6502EMULATORDLL_API void resetProcessor() {
	EmulatorState::Instance()->processor.reset_processor();
}

M6502EMULATORDLL_API void doSingleStep() {
	EmulatorState::Instance()->processor.process_instruction();
}

M6502EMULATORDLL_API void resetCycleCounter() {
	EmulatorState::Instance()->processor.resetCycleCounter();
}

M6502EMULATORDLL_API void putKeyInBuffer(uint8_t key) {
	//Invert b6, set b5 to 0.
	key = key | 0x80;

//	if ((key & 0x40) == 0x40) {
//		key = key & ~0x60;
//	}
//	else {
//		key = key | 0x60;
//	}

	MemoryMap::Instance()->write_byte(WideAddress({ 0xD0, 0x10 }), key, true);
}

M6502EMULATORDLL_API uint8_t* getMemoryRange(uint16_t base, uint16_t length) {
	uint8_t *memoryRange = new uint8_t[length+1];
	
	WideAddress start = uintAddressToWideAddress(base);
	WideAddress end = uintAddressToWideAddress(base + length);
	WideAddress current = start;
	int i = 0;
	while (current <= end && !(wideAddressToUintAddress(current) == 0x0000 && wideAddressToUintAddress(end) == 0xFFFF)) {
		memoryRange[i] = MemoryMap::Instance()->read_byte(current, false);
		i++;
		current.add(1, false);
	}

	return memoryRange;
}

M6502EMULATORDLL_API void freeBuffer(char *buffer) {
	free(buffer);
}

M6502EMULATORDLL_API void setPpuUpdatedCallback(void __stdcall callback()) {
	EmulatorState::Instance()->ppuUpdatedCallback = callback;
}