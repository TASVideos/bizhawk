NO_WBX_TARGETS := 1
CCFLAGS := -std=c99 -Wall
SRCS := amd64.c #coswap.s
#AS := nasm
#ASFLAGS := -f elf64 -Wall

include ../common.mak

$(OBJ_DIR)/%.s.o: %.s
	@echo as $<
	@mkdir -p $(@D)
	@$(AS) $(ASFLAGS) -o $@ $<
# $(DOBJ_DIR)/%.s.o: %.s
# 	@echo as $<
# 	@mkdir -p $(@D)
# 	@$(AS) $(ASFLAGS) -o $@ $<
