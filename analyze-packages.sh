#!/bin/bash

# رنگ‌ها
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m'

SOLUTION_ROOT="$(cd "$(dirname "$0")" && pwd)"

echo -e "${GREEN}🔍 Scanning for Directory.Packages.props files...${NC}"
echo ""

mapfile -t PROPS_FILES < <(find "$SOLUTION_ROOT" -name "Directory.Packages.props" -not -path "$SOLUTION_ROOT/Directory.Packages.props" -type f 2>/dev/null)

if [ ${#PROPS_FILES[@]} -eq 0 ]; then
    echo -e "${RED}No Directory.Packages.props found in subdirectories.${NC}"
    exit 1
fi

echo -e "${CYAN}Found ${#PROPS_FILES[@]} props file(s)${NC}"
echo ""

for PROPS_FILE in "${PROPS_FILES[@]}"; do
    PROPS_DIR=$(dirname "$PROPS_FILE")
    RELATIVE_DIR="${PROPS_DIR#$SOLUTION_ROOT/}"
    
    echo -e "${YELLOW}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "${YELLOW}📁 Processing: $RELATIVE_DIR/${NC}"
    echo -e "${YELLOW}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    
    mapfile -t CSPROJ_FILES < <(find "$PROPS_DIR" -maxdepth 3 -name "*.csproj" -type f 2>/dev/null)
    
    if [ ${#CSPROJ_FILES[@]} -eq 0 ]; then
        echo -e "${RED}  No .csproj files found${NC}"
        continue
    fi
    
    echo -e "${CYAN}  Projects: ${#CSPROJ_FILES[@]}${NC}"
    echo ""
    
    # ────────────────────────────────────────────
    # تحلیل پروژه‌ها و ساخت دسته‌بندی پکیج‌ها
    # ────────────────────────────────────────────
    declare -A LAYER_PACKAGES
    LAYERS=("Application" "Domain" "Infrastructure" "Persistence" "WebApi" "Test" "Other")
    for layer in "${LAYERS[@]}"; do
        LAYER_PACKAGES[$layer]=""
    done
    declare -A ALL_USED_PACKAGES
    
    for csproj in "${CSPROJ_FILES[@]}"; do
        PROJECT_NAME=$(basename "$csproj" .csproj)
        PROJECT_LAYERS=()
        if [[ "$PROJECT_NAME" == *".Application"* ]] || [[ "$PROJECT_NAME" == *"Application"* ]]; then PROJECT_LAYERS+=("Application"); fi
        if [[ "$PROJECT_NAME" == *".Domain"* ]] || [[ "$PROJECT_NAME" == *"Domain"* ]]; then PROJECT_LAYERS+=("Domain"); fi
        if [[ "$PROJECT_NAME" == *".Infrastructure"* ]] || [[ "$PROJECT_NAME" == *"Infrastructure"* ]]; then PROJECT_LAYERS+=("Infrastructure"); fi
        if [[ "$PROJECT_NAME" == *".Persistence"* ]] || [[ "$PROJECT_NAME" == *"Persistence"* ]]; then PROJECT_LAYERS+=("Persistence"); fi
        if [[ "$PROJECT_NAME" == *".WebApi"* ]] || [[ "$PROJECT_NAME" == *"WebApi"* ]] || [[ "$PROJECT_NAME" == *".Endpoint"* ]] || [[ "$PROJECT_NAME" == *"Endpoint"* ]]; then PROJECT_LAYERS+=("WebApi"); fi
        if [[ "$PROJECT_NAME" == *".Test"* ]] || [[ "$PROJECT_NAME" == *"Test"* ]] || [[ "$PROJECT_NAME" == *".Tests"* ]] || [[ "$PROJECT_NAME" == *"Tests"* ]]; then PROJECT_LAYERS+=("Test"); fi
        if [ ${#PROJECT_LAYERS[@]} -eq 0 ]; then PROJECT_LAYERS+=("Other"); fi
        
        LAYERS_STR=$(IFS=", "; echo "${PROJECT_LAYERS[*]}")
        echo -e "    📦 $PROJECT_NAME ${BLUE}→ $LAYERS_STR${NC}"
        
        while IFS= read -r pkg_name; do
            if [ -n "$pkg_name" ]; then
                for layer in "${PROJECT_LAYERS[@]}"; do
                    if [[ ! "${LAYER_PACKAGES[$layer]}" =~ (^|[[:space:]])"$pkg_name"($|[[:space:]]) ]]; then
                        LAYER_PACKAGES[$layer]+="$pkg_name "
                    fi
                done
                ALL_USED_PACKAGES[$pkg_name]=1
            fi
        done < <(grep -oP '<PackageReference\s+Include="\K[^"]+' "$csproj" 2>/dev/null)
    done
    
    echo ""
    
    # ────────────────────────────────────────────
    # استخراج نسخه‌ها از فایل props
    # ────────────────────────────────────────────
    declare -A ALL_PACKAGES
    while IFS= read -r line; do
        if [[ "$line" =~ \<PackageVersion\ Include=\"([^\"]+)\"\ Version=\"([^\"]+)\" ]]; then
            ALL_PACKAGES["${BASH_REMATCH[1]}"]="${BASH_REMATCH[2]}"
        fi
    done < "$PROPS_FILE"
    
    # ────────────────────────────────────────────
    # خواندن کل فایل و حذف ItemGroup های پکیجی
    # ────────────────────────────────────────────
    cp "$PROPS_FILE" "$PROPS_FILE.backup"
    
    # آرایه‌ای از تمام خطوط فایل
    mapfile -t lines < "$PROPS_FILE.backup"
    
    # پیدا کردن بازه‌های ItemGroup که PackageVersion دارند
    declare -A REMOVE_LINES  # کلید = شماره خط، مقدار = 1 اگر حذف شود
    in_pkg_itemgroup=false
    start_line=-1
    for i in "${!lines[@]}"; do
        line="${lines[$i]}"
        if [ "$in_pkg_itemgroup" = false ]; then
            if [[ "$line" =~ \<ItemGroup\> ]]; then
                # بررسی خط بعدی (اگر وجود داشته باشد)
                next_idx=$((i + 1))
                if [ $next_idx -lt ${#lines[@]} ]; then
                    if [[ "${lines[$next_idx]}" =~ PackageVersion ]]; then
                        in_pkg_itemgroup=true
                        start_line=$i
                    fi
                fi
            fi
        else
            # درون ItemGroup پکیجی هستیم - تا بسته شدن ادامه بده
            if [[ "$line" =~ \</ItemGroup\> ]]; then
                # پایان این ItemGroup
                for j in $(seq $start_line $i); do
                    REMOVE_LINES[$j]=1
                done
                in_pkg_itemgroup=false
            fi
        fi
    done
    
    # ────────────────────────────────────────────
    # ساخت فایل خروجی
    # ────────────────────────────────────────────
    {
        first_pkg_removed=false
        first_pkg_start=-1
        
        # چاپ خطوط غیر حذف شده
        for i in "${!lines[@]}"; do
            if [[ -z "${REMOVE_LINES[$i]}" ]]; then
                # این خط حذف نمی‌شود
                echo "${lines[$i]}"
            else
                # خط حذف شده - فقط یک بار بلوک‌های جدید را درج کن
                if [ "$first_pkg_removed" = false ]; then
                    first_pkg_removed=true
                    first_pkg_start=$i
                    # درج بلوک‌های لایه‌بندی شده
                    for layer in "${LAYERS[@]}"; do
                        if [ -n "${LAYER_PACKAGES[$layer]}" ]; then
                            echo "  <!-- ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ -->"
                            echo "  <!-- 📂 $layer Layer                                -->"
                            echo "  <!-- ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ -->"
                            echo "  <ItemGroup>"
                            
                            for pkg in $(echo "${LAYER_PACKAGES[$layer]}" | tr ' ' '\n' | sort); do
                                VERSION="${ALL_PACKAGES[$pkg]}"
                                if [ -n "$VERSION" ]; then
                                    echo "    <PackageVersion Include=\"$pkg\" Version=\"$VERSION\" />"
                                    unset ALL_PACKAGES[$pkg]
                                fi
                            done
                            
                            echo "  </ItemGroup>"
                            echo ""
                        fi
                    done
                    
                    # Unused packages
                    REMAINING=$(for pkg in "${!ALL_PACKAGES[@]}"; do echo "$pkg"; done | sort)
                    if [ -n "$REMAINING" ]; then
                        echo "  <!-- ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ -->"
                        echo "  <!-- ⚠️  Unused Packages                             -->"
                        echo "  <!-- ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ -->"
                        echo "  <ItemGroup>"
                        while IFS= read -r pkg; do
                            echo "    <PackageVersion Include=\"$pkg\" Version=\"${ALL_PACKAGES[$pkg]}\" />"
                        done <<< "$REMAINING"
                        echo "  </ItemGroup>"
                        echo ""
                    fi
                fi
                # خطوط حذف شده دیگر چاپ نمی‌شوند
            fi
        done
    } > "$PROPS_FILE"
    
    # ────────────────────────────────────────────
    # نمایش خلاصه
    # ────────────────────────────────────────────
    echo -e "${YELLOW}  📊 Summary:${NC}"
    echo ""
    for layer in "${LAYERS[@]}"; do
        if [ -n "${LAYER_PACKAGES[$layer]}" ]; then
            COUNT=$(echo "${LAYER_PACKAGES[$layer]}" | wc -w)
            echo -e "  ${BLUE}$layer${NC}: ${GREEN}$COUNT packages${NC}"
            for pkg in $(echo "${LAYER_PACKAGES[$layer]}" | tr ' ' '\n' | sort); do
                echo -e "    - $pkg"
            done
            echo ""
        fi
    done
    UNUSED_COUNT=${#ALL_PACKAGES[@]}
    if [ $UNUSED_COUNT -gt 0 ]; then
        echo -e "  ${RED}⚠️  Unused: $UNUSED_COUNT packages${NC}"
        for pkg in "${!ALL_PACKAGES[@]}"; do
            echo -e "    - $pkg"
        done
        echo ""
    fi
    
    unset LAYER_PACKAGES
    unset ALL_PACKAGES
    unset ALL_USED_PACKAGES
    declare -A LAYER_PACKAGES
    declare -A ALL_PACKAGES
    declare -A ALL_USED_PACKAGES
done

echo -e "${GREEN}✅ All done!${NC}"
echo -e "${YELLOW}💡 .backup files created${NC}"