# Orca-Flashforge
Orca-Flashforge is an open source slicer for FDM printers.

# How to compile
- Windows 64-bit  
  - Tools needed: Visual Studio 2022, CMake, Git, Strawberry Perl.
  - Run `build_release.bat` in `x64 Native Tools Command Prompt for VS 2022`

- Mac 64-bit  
  - Tools needed: Xcode, CMake, Git, gettext, Automake, Perl
  - run `build_release_macos.sh`

# License
Orca-Flashforge is licensed under the GNU Affero General Public License, version 3. Orca-Flashforge is based on Orca Slicer by SoftFever.

Orca Slicer is licensed under the GNU Affero General Public License, version 3. Orca Slicer is based on Bambu Studio by BambuLab.

Bambu Studio is licensed under the GNU Affero General Public License, version 3. Bambu Studio is based on PrusaSlicer by PrusaResearch.

PrusaSlicer is licensed under the GNU Affero General Public License, version 3. PrusaSlicer is owned by Prusa Research. PrusaSlicer is originally based on Slic3r by Alessandro Ranellucci.

Slic3r is licensed under the GNU Affero General Public License, version 3. Slic3r was created by Alessandro Ranellucci with the help of many other contributors.

The GNU Affero General Public License, version 3 ensures that if you use any part of this software in any way (even behind a web server), your software must be released under the same license.

Orca-Flashforge includes a pressure advance calibration pattern test adapted from Andrew Ellis' generator, which is licensed under GNU General Public License, version 3. Ellis' generator is itself adapted from a generator developed by Sineos for Marlin, which is licensed under GNU General Public License, version 3.

The flashforge networking plugin is based on non-free libraries from FlashForge. It is optional to the Orca-Flashforge and provides extended functionalities for FlashForge printer users.

---

# Mejoras y Correcciones para Linux (Fork de zamora322)

Este fork mantiene correcciones críticas para la ejecución, visualización y estabilidad de Orca-Flashforge en distribuciones modernas de Linux (como Fedora 40+, Ubuntu 24.04+, Arch Linux, entornos Wayland y drivers gráficos NVIDIA / Mesa Zink):

### 1. Corrección de cuelgue por contexto OpenGL y miniaturas (SIGSEGV)
- **Problema:** La aplicación se cerraba inesperadamente al cambiar de pestaña o invocar la generación de miniaturas (`render_thumbnail`) debido a llamadas a buffers OpenGL sin un contexto gráfico activo y ligado al hilo actual.
- **Solución:** Se implementó verificación y enlace explícito del contexto OpenGL visible (`_set_shown_canvas_current`) en `GLCanvas3D.cpp`, evitando accesos ilegales a memoria.

### 2. Corrección del bloqueo de renderizado en la pestaña "Preparar" (Wayland)
- **Problema:** En entornos Wayland, la ventana se expone de manera asíncrona, provocando que `IsShownOnScreen()` devolviera `false` durante la inicialización (`post_init`). La aplicación deshabilitaba el renderizado con `enable_render(false)` y nunca lo restauraba, dejando la pestaña de preparación completamente en blanco/sin dibujar el modelo ni la cama.
- **Solución:** Se eliminó la supresión forzada de renderizado en Linux en `GUI_App.cpp` y se aseguró la reactivación automática de `m_enable_render = true` en `GLCanvas3D.cpp` en cuanto el lienzo se muestra en pantalla y OpenGL está listo.

### 3. Corrección de invisibilidad de modelos 3D y descarte en Shaders (SPIR-V / Mesa Zink)
- **Problema:** Al enviar cotas infinitas (`DBL_MAX` y `FLT_MAX`) para indicar la ausencia de planos de corte, los flotantes de 32 bits en la GPU desbordaban a `+INFINITY`. Durante la rasterización por hardware en Mesa Zink (SPIR-V), la interpolación baricéntrica producía valores `NaN`, haciendo que la condición `lessThan(clipping_planes_dots, ZERO)` evaluara como verdadera y descartara (`discard;`) el 100% de los fragmentos del modelo 3D en la pestaña "Preparar" y en vistas de sección.
- **Solución:** Se acotaron los uniformes en `GLShader.cpp` y `GLCanvas3D.cpp` y se añadieron comprobaciones de inactividad de planos de corte (`>= 1e5` y `<= -1e5`) en los shaders GLSL (`gouraud`, `mm_gouraud` y `flat_clip` en versiones 140 y 110), reemplazando comparaciones no ordenadas por verificaciones seguras.

### 4. Corrección de fallo de segmentación en notificaciones de laminado (`ImFont::CalcTextSizeA`)
- **Problema:** Al iniciar el laminado, los eventos de progreso en segundo plano actualizaban las notificaciones flotantes en el bucle inactivo de wxWidgets. Al llamar a `count_spaces()` y `count_lines()`, se invocaba `ImGui::CalcTextSize()` fuera de un fotograma de ImGui, donde `ImGui::GetFont()` es nulo, causando un `SIGSEGV` inmediato.
- **Solución:** Se introdujeron comprobaciones de validez de contexto y fuente (`ImGui::GetCurrentContext()` y `ImGui::GetFont()`) en `NotificationManager.cpp` y `HintNotification.cpp`, asignando dimensiones seguras de respaldo mientras el contexto gráfico no esté en un pase de renderizado activo.

### 5. Corrección de inicio de sesión y servicios FlashForge Cloud (`libFlashNetwork.so`)
- **Problema:** Tras autenticarse vía web (Google OAuth), el inicio de sesión no persistía o quedaba congelado en el mensaje *"Inicio de sesión..."* / *"Account Auto Connecting..."*. Esto se debía a que el módulo de comunicaciones de Flashforge (`libFlashNetwork.so`) no se empaquetaba dentro del AppImage, y la clase `FlashNetworkIntfc` exigía de forma estricta la versión `3.4.3` y métodos nuevos de sincronización de perfiles en la nube ausentes en Linux, haciendo fallar la inicialización con error `stage=network_interface`.
- **Solución:** Se integró la biblioteca `libFlashNetwork.so` en el repositorio y en los scripts de empaquetado del AppImage (`build_linux.sh` y `build_linux_image.sh.in`), se corrigió la detección de plataforma en `GUI_App.cpp` (`defined(__linux__)`), se implementaron funciones de respaldo (*fallbacks*) para las llamadas opcionales de sincronización y se flexibilizó la comprobación de versión para admitir la serie `3.4.x`. Con esto, la sesión de usuario, el perfil y la comunicación con impresoras en la nube se activan inmediatamente.

---

### Compilación en Linux

Para compilar el proyecto y empaquetar el AppImage en Linux utilizando el contenedor runner:

```bash
./build_linux.sh -g -sirlL -j $(nproc)
```

El AppImage resultante se genera en el directorio `build/` (ej. `build/Flash Studio_linux_V1.7.15.AppImage`).
